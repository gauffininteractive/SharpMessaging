using System;
using System.Collections.Generic;
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
using SharpMessaging.Persistence;

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
        private Type _inboundDotNetType;
        private IPayloadSerializer _payloadSerializer;
        private ushort _sequenceCounter = 0;
        private ClientState _state;
        private IQueueStorage _messageStore;
        private DotNetTypeExtension _dotNetExtension;
        private Type _outboundDotNetType;


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
            _connection.Fault += OnConnectionFault;
        }

        private void OnConnectionFault(object sender, FaultExceptionEventArgs e)
        {
            Console.WriteLine(e.ErrorMessage);
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

            // using acks and the specified frame have already been recieved.
            if (_ackSender != null && _ackSender.ShouldReAck(frame))
            {
                _ackSender.AckFrame(frame);
                return;
            }
                
            if (_payloadSerializer != null)
            {
                if (_inboundDotNetType != null)
                {
                    frame.Payload = frame.IsFlaggedAsSmall
                        ? _payloadSerializer.Deserialize(_inboundDotNetType, frame.PayloadBuffer.Array,
                            frame.PayloadBuffer.Offset,
                            frame.PayloadBuffer.Count)
                        : _payloadSerializer.Deserialize(_inboundDotNetType, frame.PayloadStream);
                }
                else
                {
                    frame.Payload = frame.IsFlaggedAsSmall
                        ? _payloadSerializer.Deserialize(frame.PayloadBuffer.Array,
                            frame.PayloadBuffer.Offset,
                            frame.PayloadBuffer.Count)
                        : _payloadSerializer.Deserialize(frame.PayloadStream);
                }
            }
         

            FrameReceived(frame);

            //Do it after the event trigger, so that any exception
            //doesn't ack the frame (as the client did not process it correctly).
            if (_ackSender != null)
                _ackSender.AckFrame(frame);
        }

        public void AckFrame(MessageFrame frame)
        {
            var ack = new AckFrame(_ackExtensionId, frame.SequenceNumber);
            _connection.Send(ack);
        }

        public void Send(MessageFrame frame)
        {
            if (frame == null) throw new ArgumentNullException("frame");

            //if (!_authenticationEvent.WaitOne(100000))
            //    throw new InvalidOperationException("Handshake was not completed in a reasonable time.");

            if (_ackReceiver != null)
            {
                // we can allow all requests to send messages
                // if 
                if (_messageStore != null)
                {
                    _messageStore.Enqueue(frame);
                    if (_ackReceiver.CanSend(frame))
                        _ackReceiver.Send(frame);
                }
                else
                {
                    if (!_ackReceiver.CanSend(frame))
                        throw new InvalidOperationException("Cannot enqueue more messages that the given threshold.");
                    _ackReceiver.Send(frame);
                }
            }
            else
                DeliverMessage(frame);
        }

        private void DeliverMessage(MessageFrame frame)
        {
            frame.SequenceNumber = ++_sequenceCounter;
            if (_sequenceCounter == ushort.MaxValue)
                _sequenceCounter = 0;

            if (frame.Payload != null && _payloadSerializer != null)
            {
                _payloadSerializer.Serialize(frame);
                if (_dotNetExtension != null && _outboundDotNetType != frame.Payload.GetType())
                {
                    _outboundDotNetType = frame.Payload.GetType();
                    var dotNetFrame = _extensionService.CreateFrame("dotnet", _outboundDotNetType);
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

            //TODO: This is a mess. Create a better way
            // to identify and activate extensions.
            // maybe by defining extension behavior like IInboundMessageProcessor.

            var id = _extensionService.FindFirstExtensionId("json", "xml", "protobuf");
            if (id > 0)
                _payloadSerializer = (((IPayloadExtension)_extensionService.Get(id))).CreatePayloadSerializer();

            id = _extensionService.FindFirstExtensionId("dotnet");
            if (id > 0)
                _dotNetExtension = (DotNetTypeExtension) _extensionService.Get(id);

            _ackExtensionId = _extensionService.FindFirstExtensionId("batch-ack", "ack");
            if (_ackExtensionId != 0)
            {
                var name = _extensionService.FindFirstExtensionNamed("batch-ack", "ack");
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
                _inboundDotNetType = ((DotNetType) frame.Payload).CreateType();
            }
            if (frame.ExtensionId == _ackExtensionId)
            {
                var ackCount = _ackReceiver.Confirm((AckFrame) frame);
                if (_messageStore != null)
                {
                    _messageStore.Remove(ackCount);
                    var msgsToSend = new List<object>();
                    _messageStore.Peek(msgsToSend, _ackReceiver.FreeSlots);
                    foreach (var o in msgsToSend)
                    {
                        //TODO: We should really send a list so that SendMore() can be sued
                        _ackReceiver.Send((MessageFrame)o);    
                    }
                }
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