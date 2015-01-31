using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SharpMessaging.Extensions;
using SharpMessaging.Frames;

namespace SharpMessaging.Connection
{
    public class Connection : IConnection
    {
        public const byte Major = 1;
        public const byte Minor = 1;
        private readonly ExtensionFrameProcessor _extensionFrameProcessor;
        private readonly HandshakeFrame _handshakeFrame = new HandshakeFrame();
        private readonly string _identity;
        private readonly MessageFrame _inboundMessage = new MessageFrame();
        private readonly SocketAsyncEventArgs _readArgs;
        private readonly SocketAsyncEventArgs _writeArgs;
        private readonly CircularQueueList<IBufferWriter> _writeQueue = new CircularQueueList<IBufferWriter>(1000000);
        private readonly WriterContext _writerContext;
        ErrorFrame _errorFrame = new ErrorFrame(null);
        public Action<ExtensionFrame> ExtensionFrameReceived;
        public Action<MessageFrame> MessageFrameReceived;
        public Action<int> WriteCompleted;
        private FrameType _frameType;
        private bool _handshakeCompleted;
        private int _pendingWriteBufferCount;
        private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private int _writerCount;
        private bool _bufferAllocated;

        public Connection(string identity, IExtensionService extensionRegistry, bool isServer,
            BufferManager bufferManager)
        {
            _identity = identity ?? "FastSocket v" + Major + "" + Minor;
            _readArgs = new SocketAsyncEventArgs();
            _readArgs.Completed += OnReadCompleted;

            _writeArgs = new SocketAsyncEventArgs();
            _writeArgs.Completed += OnWriteCompleted;
            //_writeArgs.SendPacketsFlags = TransmitFileOptions.UseKernelApc;

            _extensionFrameProcessor = new ExtensionFrameProcessor(extensionRegistry.CreateFrame, OnExtension);
            _writerContext = new WriterContext(bufferManager);

            ExtensionFrameReceived = frame => { };
            HandshakeReceived = (o, e) => { };
            MessageFrameReceived = frame => { };
            WriteCompleted = i => { };
            Disconnected = (o, e) => { };
            Fault = (o, e) => { };
        }


        public string Identity
        {
            get { return _identity; }
        }

        public int SendBufferSize { get; set; }
        public int ReceiveBufferSize { get; set; }

        public IPEndPoint RemoteEndPoint
        {
            get { return (IPEndPoint)_socket.RemoteEndPoint; }
        }

        public void Send(IFrame frame)
        {
            if (Interlocked.CompareExchange(ref _writerCount, 1, 0) == 0)
            {
                _writeQueue.Enqueue(frame);
                SendQueuedBuffers();
            }
            else
            {
                _writeQueue.Enqueue(frame);
            }
        }

        /// <summary>
        ///     Enqueue message, but do not initiate the Send procedure yet.
        /// </summary>
        /// <param name="frame"></param>
        public void SendMore(IFrame frame)
        {
            _writeQueue.Enqueue(frame);
        }

        public void Start(string remoteAddress, int port)
        {
            _socket.Connect(remoteAddress, port);

            AllocateBuffers();

            var isPending = _socket.ReceiveAsync(_readArgs);
            if (!isPending)
                ProcessReceivedBytes(_readArgs.Buffer, _readArgs.Offset, _readArgs.BytesTransferred);
        }

        public void Start(IPAddress remoteAddress, int port)
        {
            _socket.Connect(remoteAddress, port);

            AllocateBuffers();

            var isPending = _socket.ReceiveAsync(_readArgs);
            if (!isPending)
                ProcessReceivedBytes(_readArgs.Buffer, _readArgs.Offset, _readArgs.BytesTransferred);
        }

        public void Assign(Socket socket)
        {
            _socket = socket;
            AllocateBuffers();

            var isPending = _socket.ReceiveAsync(_readArgs);
            if (!isPending)
                ProcessReceivedBytes(_readArgs.Buffer, _readArgs.Offset, _readArgs.BytesTransferred);
        }

        public event EventHandler<FaultExceptionEventArgs> Fault = delegate { };
        public event EventHandler<HandshakeFrameReceivedEventArgs> HandshakeReceived;
        public event EventHandler<DisconnectedEventArgs> Disconnected = delegate { };

        private void OnExtension(ExtensionFrame obj)
        {
            ExtensionFrameReceived(obj);
        }

        public void SendErrorFrame(string errorMessage)
        {
            //_state = ClientState.Error;
            _socket.Shutdown(SocketShutdown.Receive);
        }

        public void SetHandshakeCompleted()
        {
            _handshakeCompleted = true;
        }

        private void AllocateBuffers()
        {
            if (_bufferAllocated)
                return;
            _bufferAllocated = true;
            if (SendBufferSize <= 0)
            {
                SendBufferSize = (int) _socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer);
            }
            else
            {
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, SendBufferSize);
                _writeArgs.SetBuffer(new byte[SendBufferSize], 0, SendBufferSize);
            }

            if (ReceiveBufferSize <= 0)
            {
                ReceiveBufferSize =
                    (int) _socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
                _readArgs.SetBuffer(new byte[ReceiveBufferSize], 0, ReceiveBufferSize);
            }
            else
            {
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, ReceiveBufferSize);
                _readArgs.SetBuffer(new byte[ReceiveBufferSize], 0, ReceiveBufferSize);
            }
        }

        private void HandleRemoteDisconnect(SocketError socketError)
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Disconnect(false);
                Disconnected(this, new DisconnectedEventArgs(socketError));
            }
            catch (Exception ex)
            {
                Fault(this, new FaultExceptionEventArgs("Failed to disconnect successfully", ex));
            }
        }

        private void OnReadCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 0 || e.SocketError != SocketError.Success)
            {
                HandleRemoteDisconnect(e.SocketError);
                return;
            }

            var isPending = false;
            try
            {
                ProcessReceivedBytes(e.Buffer, e.Offset, e.BytesTransferred);
                isPending = _socket.ReceiveAsync(_readArgs);
            }
            catch (Exception exception)
            {
                Fault(this,
                    new FaultExceptionEventArgs("Failed to process " + e.BytesTransferred + " incoming bytes.",
                        exception));
            }


            if (!isPending)
                OnReadCompleted(sender, _readArgs);
        }

        private void OnWriteCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
            {
                //let receive side shut down the connection.
                return;
            }

            WriteCompleted(e.BytesTransferred);
            SendQueuedBuffers();
        }

        private void ProcessReceivedBytes(byte[] buffer, int offset, int bytesTransferred)
        {
            while (bytesTransferred > 0)
            {
                if (!_handshakeCompleted)
                {
                    var flags = (FrameFlags)buffer[offset];
                    if ((flags & FrameFlags.ErrorFrame) != 0)
                    {
                        _frameType = FrameType.Error;
                        _handshakeCompleted = true;
                        continue;
                    }

                    var isCompleted = _handshakeFrame.Read(buffer, ref offset, ref bytesTransferred);
                    if (!isCompleted)
                    {
                        return;
                    }
                    HandshakeReceived(this, new HandshakeFrameReceivedEventArgs(_handshakeFrame));
                    _handshakeFrame.ResetRead();
                    continue;
                }

                switch (_frameType)
                {
                    case FrameType.Unknown:
                        var flags = (FrameFlags) buffer[offset];
                        if ((flags & FrameFlags.ErrorFrame) != 0)
                        {
                            _frameType = FrameType.Error;
                        }
                        if ((flags & FrameFlags.ExtensionFrame) != 0)
                        {
                            _frameType = FrameType.Extension;
                        }
                        else if ((flags & FrameFlags.CommandFrame) != 0)
                        {
                            _frameType = FrameType.Command;
                        }
                        else
                            _frameType = FrameType.Message;

                        //do not increase offset, let the frame handle the flags.
                        //only peek it to be able to switch state
                        break;
                    case FrameType.Message:
                        var isCompleted = _inboundMessage.Read(buffer, ref offset, ref bytesTransferred);
                        if (isCompleted)
                        {
                            MessageFrameReceived(_inboundMessage);
                            _inboundMessage.ResetRead();
                            _frameType = FrameType.Unknown;
                        }

                        break;

                    case FrameType.Error:
                        var isCompleted1 = _errorFrame.Read(buffer, ref offset, ref bytesTransferred);
                        if (!isCompleted1)
                        {
                            Fault(this, new FaultExceptionEventArgs(_errorFrame.ErrorMessage, new RemoteEndPointException(_errorFrame.ErrorMessage)));
                            Close();
                        }
                        break;

                    case FrameType.Extension:
                        var isCompleted2 = _extensionFrameProcessor.Read(buffer, ref offset, ref bytesTransferred);
                        if (isCompleted2)
                            _frameType = FrameType.Unknown;
                        break;
                }
            }
        }

        /// <summary>
        ///     Do note that this MUST only be done from the writer thread
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The
        ///     </para>
        /// </remarks>
        private void SendQueuedBuffers()
        {
            // all of this mess is because we want to be able to return buffers (writer.Reset) when a send operation has been completed.
            // a better solution would have been to have a single circular list instead.
            if (_pendingWriteBufferCount > 0)
            {
                while (_pendingWriteBufferCount > 1)
                {
                    var buffer = _writeQueue.Dequeue();
                    buffer.ResetWrite(_writerContext);
                    --_pendingWriteBufferCount;
                }
                _writerContext.Reset();
                if (_writerContext.IsPartial)
                {
                    _writerContext.IsPartial = !_writeQueue[0].Write(_writerContext);
                    if (_writerContext.IsPartial)
                    {
                        return;
                    }
                }
                else
                {
                    var buffer = _writeQueue.Dequeue();
                    buffer.ResetWrite(_writerContext);
                    --_pendingWriteBufferCount;
                }

                if (_writeQueue.Count == 0)
                {
                    Interlocked.Exchange(ref _writerCount, 0);
                    return;
                }
            }

            //max to prevent a blue screen.
            var max = 400;
            var queueCount = _writeQueue.Count;
            //no lock as this is logic is always run at a single thread at a time.
            // and the only thread tha dequeues.
            while (queueCount > _pendingWriteBufferCount && max-- > 0 && _writerContext.BytesLeftToEnqueue > 30)
            {
                var buffer = _writeQueue[_pendingWriteBufferCount++];
                _writerContext.IsPartial = !buffer.Write(_writerContext);
                if (_writerContext.IsPartial)
                {
                    break;
                }
            }

            _writeArgs.SendPacketsElements = _writerContext.GetPackets().ToArray();
            if (_writeArgs.SendPacketsElements.Length == 0)
            {
                _writerContext.Reset();
                Interlocked.Exchange(ref _writerCount, 0);
                return;
            }

            //foreach (var element in _writeArgs.SendPacketsElements)
            //{
            //    LazyDebugWrite("SEND [" + element.Offset + "," + element.Count + "] -> " + string.Join(",", element.Buffer.Skip(element.Offset).Take(element.Count)));
            //}

            var isPending = _socket.SendPacketsAsync(_writeArgs);
            if (!isPending)
                OnWriteCompleted(this, _writeArgs);
        }

        public void Reset()
        {
            _handshakeFrame.ResetRead();
            _handshakeFrame.ResetWrite(_writerContext);
            _writeQueue.Clear();
            _frameType= FrameType.Unknown;
            _inboundMessage.ResetRead();
            _pendingWriteBufferCount = 0;
            Interlocked.CompareExchange(ref _writerCount, 0, 1);
            _handshakeCompleted = false;
            _extensionFrameProcessor.ResetRead();


        }

        public void Close()
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
    }
}