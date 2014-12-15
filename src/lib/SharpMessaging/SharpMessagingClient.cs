using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using SharpMessaging.Connection;
using SharpMessaging.Extensions;
using SharpMessaging.Extensions.Ack;
using SharpMessaging.Extensions.Payload.DotNet;
using SharpMessaging.Frames;
using SharpMessaging.Frames.Extensions;
using SharpMessaging.Payload;

namespace SharpMessaging
{
    public class SharpMessagingClient
    {
        private readonly ManualResetEventSlim _authenticationEvent = new ManualResetEventSlim(false);
        private readonly BufferManager _bufferManager = new BufferManager(65535, 1000);
        private readonly Connection.Connection _connection;
        private readonly IExtensionService _extensionService;
        public Action<AckFrame> AckReceived;
        public Action<MessageFrame> FrameReceived;
        private byte _ackExtensionId;
        private IAckReceiver _ackReceiver;
        private IAckSender _ackSender;
        private Type _payloadDotNetType;
        private FastJsonSerializer _payloadSerializer;
        private ushort _sequenceCounter = 0;
        private ClientState _state;


        public SharpMessagingClient(string identity, IExtensionRegistry extensionRegistry)
        {
            _extensionService = new ExtensionService(extensionRegistry, DeliverMessage);
            _state = ClientState.ClientToServerHandshake1;
            _connection = new Connection.Connection(identity, _extensionService, false, _bufferManager)
            {
                ExtensionFrameReceived = OnExtensionFrame,
                MessageFrameReceived = OnMessageFrame,
                WriteCompleted = OnWriteCompleted,
                //ReceiveBufferSize = 65535
            };
            _connection.HandshakeReceived += OnServerHandshakeFrame;
            _connection.Disconnected += OnDisconnected;
        }

        private void OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            
        }

        public SharpMessagingClient()
            : this("SharpMessaging", new ExtensionRegistry())
        {
        }

        private void OnWriteCompleted(int bytesTransferred)
        {
        }

        private void OnMessageFrame(MessageFrame frame)
        {
            if (_state != ClientState.Ready)
                throw new Exception("Handshake not completed, should not have received a message frame.");

            if (_payloadSerializer == null)
            {
                FrameReceived(frame);
                return;
            }

            if (_payloadDotNetType != null)
            {
                frame.Payload = frame.IsFlaggedAsSmall
                    ? _payloadSerializer.Deserialize(_payloadDotNetType, frame.PayloadBuffer.Array,
                        frame.PayloadBuffer.Offset,
                        frame.PayloadBuffer.Count)
                    : _payloadSerializer.Deserialize(_payloadDotNetType, frame.PayloadStream);
            }
            else
            {
                frame.Payload = frame.IsFlaggedAsSmall
                    ? _payloadSerializer.Deserialize(frame.PayloadBuffer.Array,
                        frame.PayloadBuffer.Offset,
                        frame.PayloadBuffer.Count)
                    : _payloadSerializer.Deserialize(frame.PayloadStream);
            }

            FrameReceived(frame);
        }

        public void AckFrame(MessageFrame frame)
        {
            var ack = new AckFrame(_ackExtensionId, frame.SequenceNumber);
            _connection.Send(ack);
        }

        public void Send(MessageFrame frame)
        {
            //if (!_authenticationEvent.WaitOne(100000))
            //    throw new InvalidOperationException("Handshake was not completed in a reasonable time.");

            if (frame.PayloadBuffer.Count == 0)
                Debugger.Break();

            frame.SequenceNumber = ++_sequenceCounter;
            if (_sequenceCounter == ushort.MaxValue)
                _sequenceCounter = 0;

            if (_ackReceiver != null)
                _ackReceiver.AddFrame(frame);
            else
                DeliverMessage(frame);
        }

        private void DeliverMessage(MessageFrame frame)
        {
            if (frame.PayloadBuffer.Count == 0)
                Debugger.Break();

            if (frame.Payload != null && _payloadSerializer != null)
            {
                _payloadSerializer.Serialize(frame);
                if (_payloadDotNetType != frame.Payload.GetType())
                {
                    _payloadDotNetType = frame.Payload.GetType();
                    var dotNetFrame = _extensionService.CreateFrame("dotnet", _payloadDotNetType);
                    _connection.SendMore(dotNetFrame);
                }
            }
            _connection.Send(frame);
        }

        private void OnServerHandshakeFrame(object source, HandshakeFrameReceivedEventArgs e)
        {
            if (_state != ClientState.ServerToClientHandshake)
                throw new Exception("Server handshake should not be received during " + _state);
            var frame = _extensionService.ClientConfirmExtensions(e.Handshake, _connection.Identity);

            _state = ClientState.Ready;
            _connection.SetHandshakeCompleted();
            _connection.Send(frame);


            var name = _extensionService.FindFirstExtensionNamed("json", "xml", "protobuf");
            switch (name)
            {
                case "json":
                    _payloadSerializer = new FastJsonSerializer();
                    break;
            }

            _ackExtensionId = _extensionService.FindFirstExtensionId("batch-ack", "ack");
            if (_ackExtensionId != 0)
            {
                name = _extensionService.FindFirstExtensionNamed("batch-ack", "ack");
                var extProperties = frame.GetExtension(name);
                var ackExtension = (IAckExtension) _extensionService.Get(_ackExtensionId);
                _ackReceiver = ackExtension.CreateAckReceiver(_connection, _ackExtensionId, DeliverMessage,
                    extProperties);
                _ackSender = ackExtension.CreateAckSender(_connection, _ackExtensionId, extProperties);
            }


            _authenticationEvent.Set();
        }


        private void OnExtensionFrame(ExtensionFrame frame)
        {
            if (frame.Payload is DotNetType)
            {
                _payloadDotNetType = ((DotNetType) frame.Payload).CreateType();
            }
            if (frame.ExtensionId == _ackExtensionId)
            {
                _ackReceiver.Confirm((AckFrame) frame);
                if (AckReceived != null)
                    AckReceived((AckFrame) frame);
            }
        }

        public void Start(IPEndPoint endPoint)
        {
            _connection.Start(endPoint.Address, endPoint.Port);

            var frame = _extensionService.CreateClientHandshake(_connection.Identity);
            _state = ClientState.ServerToClientHandshake;
            ThreadPool.QueueUserWorkItem(x => _connection.Send(frame));
            _authenticationEvent.Wait(100000);
        }

        public void Start(string endPoint, int port)
        {
            _connection.Start(endPoint, port);

            var frame = _extensionService.CreateClientHandshake(_connection.Identity);
            _state = ClientState.ServerToClientHandshake;
            ThreadPool.QueueUserWorkItem(x => _connection.Send(frame));
            _authenticationEvent.Wait(100000);
        }

        public void Close()
        {
            _connection.Close();
        }
    }
}