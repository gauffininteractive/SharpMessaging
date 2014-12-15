using System;
using System.Net;
using System.Net.Sockets;
using SharpMessaging.Connection;
using SharpMessaging.Extensions;
using SharpMessaging.Extensions.Ack;
using SharpMessaging.Extensions.Payload.DotNet;
using SharpMessaging.Frames;
using SharpMessaging.Frames.Extensions;
using SharpMessaging.Payload;

namespace SharpMessaging.Server
{
    public class ServerClient
    {
        public const byte Major = 1;
        public const byte Minor = 1;
        private readonly BufferManager _bufferManager;
        private readonly Connection.Connection _connection;
        private readonly IExtensionService _extensionService;
        public Action<ServerClient, MessageFrame> FrameReceived;
        public Action HandshakeCompleted;
        private IAckReceiver _ackReceiver;
        private IAckSender _ackSender;
        private Type _payloadDotNetType;
        private IPayloadSerializer _payloadSerializer;
        private ushort _sequenceCounter;
        private ServerState _state;

        public ServerClient(string identity, IExtensionRegistry extensionRegistry, BufferManager bufferManager)
        {
            ServerName = identity;
            _bufferManager = bufferManager;
            _extensionService = new ExtensionService(extensionRegistry, DeliverMessage);
            _connection = new Connection.Connection(ServerName, _extensionService, true, _bufferManager)
            {
                ExtensionFrameReceived = OnExtensionFrame,
                MessageFrameReceived = OnMessageFrame,
                WriteCompleted = OnWriteCompleted,
                
            };
            _connection.HandshakeReceived += OnHandshakeFrame;
            _connection.Disconnected += HandleRemoteDisconnect;
            _state = ServerState.WaitingOnInitialHandshake;
            ServerName = "FastSocket v" + Major + "." + Minor;
        }

        public string ClientName { get; set; }
        public string ServerName { get; set; }

        public IPEndPoint RemoteEndPoint
        {
            get { return _connection.RemoteEndPoint; }
        }

        private void OnWriteCompleted(int obj)
        {
            if (_state != ServerState.Ready)
                return;
        }

        private void HandleRemoteDisconnect(object sender, DisconnectedEventArgs e)
        {
            _connection.Reset();
            Disconnected(this, e);
        }

        public event EventHandler<DisconnectedEventArgs> Disconnected; 

        public void Send(MessageFrame frame)
        {
            frame.SequenceNumber = ++_sequenceCounter;
            if (_sequenceCounter == ushort.MaxValue)
                _sequenceCounter = 0;

            if (_ackReceiver != null)
                _ackReceiver.AddFrame(frame);
            else
                DeliverMessage(frame);
        }

        /// <summary>
        ///     Message have been queued etc and are now OK for delivery
        /// </summary>
        /// <param name="frame"></param>
        private void DeliverMessage(MessageFrame frame)
        {
            //_extensionService.Prepare(frame, _connection);
            if (frame.Payload != null && _payloadDotNetType != frame.Payload.GetType())
            {
                _payloadDotNetType = frame.Payload.GetType();
                var dotNetFrame = _extensionService.CreateFrame("dotnet", _payloadDotNetType);
                _connection.SendMore(dotNetFrame);
            }
            if (_payloadSerializer != null)
            {
                _payloadSerializer.Serialize(frame);
            }

            _connection.Send(frame);
        }

        private void OnExtensionFrame(ExtensionFrame obj)
        {
            if (obj.Payload is DotNetType)
            {
                var type = (DotNetType) obj.Payload;
                _payloadDotNetType = type.CreateType();
            }
        }

        private void OnHandshakeFrame(object source, HandshakeFrameReceivedEventArgs e)
        {
            Console.WriteLine(e.Handshake);
            switch (_state)
            {
                case ServerState.WaitingOnInitialHandshake:
                    NegotiateHandshake(e.Handshake);
                    break;

                case ServerState.WaitingOnFinalHandshake:
                    FinalizeHandshake(e.Handshake);
                    break;
            }
        }

        private void OnMessageFrame(MessageFrame frame)
        {
            if (_state != ServerState.Ready)
                throw new Exception("Handshake not completed, should not have received a message frame.");


            if (_payloadSerializer != null)
            {
                if (_payloadDotNetType != null)
                {
                    frame.Payload = frame.PayloadStream == null
                        ? _payloadSerializer.Deserialize(_payloadDotNetType, frame.PayloadBuffer.Array,
                            frame.PayloadBuffer.Offset,
                            frame.PayloadBuffer.Count)
                        : _payloadSerializer.Deserialize(_payloadDotNetType, frame.PayloadStream);
                }
                else
                {
                    frame.Payload = frame.PayloadStream == null
                        ? _payloadSerializer.Deserialize(frame.PayloadBuffer.Array,
                            frame.PayloadBuffer.Offset,
                            frame.PayloadBuffer.Count)
                        : _payloadSerializer.Deserialize(frame.PayloadStream);
                }
            }

            if (_ackSender == null || _ackSender.AddFrame(frame))
                FrameReceived(this, frame);
        }

        private void NegotiateHandshake(HandshakeFrame handshakeFrame)
        {
            ClientName = handshakeFrame.Identity;
            var frame = _extensionService.ServerNegotiate(handshakeFrame, ServerName);
            _state = ServerState.WaitingOnFinalHandshake;
            _connection.Send(frame);
        }

        private void FinalizeHandshake(HandshakeFrame handshakeFrame)
        {
            _extensionService.ServerConfirm(handshakeFrame);
            _connection.SetHandshakeCompleted();
            _state = ServerState.Ready;
            var extensionId = _extensionService.FindFirstExtensionId("batch-ack", "ack");
            if (extensionId != 0)
            {
                var name = _extensionService.FindFirstExtensionNamed("batch-ack", "ack");
                var extProperties = handshakeFrame.GetExtension(name);
                var ackExtension = (IAckExtension) _extensionService.Get(extensionId);
                _ackReceiver = ackExtension.CreateAckReceiver(_connection, extensionId, DeliverMessage, extProperties);
                _ackSender = ackExtension.CreateAckSender(_connection, extensionId, extProperties);
            }


            extensionId = _extensionService.FindFirstExtensionId("json", "protobuf", "xml");
            if (extensionId != 0)
            {
                var payloadExtension = (IPayloadExtension) _extensionService.Get(extensionId);
                _payloadSerializer = payloadExtension.CreatePayloadSerializer();
            }


            if (HandshakeCompleted != null)
                HandshakeCompleted();
        }

        public void Start(Socket socket)
        {
            _connection.Assign(socket);
            
        }

        public void Reset()
        {
            //these will be re-negotiated
            _ackReceiver.Dispose();
            _ackReceiver = null;
            _ackSender.Dispose();
            _ackSender = null;

            _connection.Reset();
            _extensionService.Reset();
            _sequenceCounter = 0;
            _state = ServerState.WaitingOnInitialHandshake;
            
        }
    }
}