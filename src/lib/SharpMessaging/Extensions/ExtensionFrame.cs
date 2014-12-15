using System;
using System.Runtime.CompilerServices;
using SharpMessaging.Connection;
using SharpMessaging.Frames;

namespace SharpMessaging.Extensions
{
    public abstract class ExtensionFrame : IFrame
    {
        private readonly byte _extensionId;
        private readonly byte[] _receiveBuffer = new byte[256];
        private FrameFlags _flags;
        private int _receiveBufferOffset = 0;
        private ExtensionFrameState _receiveState = ExtensionFrameState.Flags;
        private int _receiveStateBufferLeft = 4;
        private ArraySegment<byte> _sendBuffer = WriterContext.EmptySegment;
        private bool _sendCompleted;

        protected ExtensionFrame(byte extensionId)
        {
            _extensionId = extensionId;
            _flags = FrameFlags.ExtensionFrame;
            _receiveState = ExtensionFrameState.PayloadLength;
            _sendCompleted = true;
        }

        public byte ExtensionId
        {
            get { return _extensionId; }
        }

        private bool IsSmallPayload
        {
            get { return (_flags & FrameFlags.LargeFrame) == 0; }
        }

        /// <summary>
        ///     Content that the extension frame held.
        /// </summary>
        public object Payload { get; set; }

        /// <summary>
        ///     Connection failed. Reset state until  a new connection is received.
        /// </summary>
        public void ResetRead()
        {
        }

        public bool Read(byte[] buffer, ref int offset, ref int bytesTransferred)
        {
            while (bytesTransferred > 0)
            {
                switch (_receiveState)
                {
                    case ExtensionFrameState.PayloadLength:
                        if (IsSmallPayload)
                        {
                            SetReceivePayloadLength(buffer[offset]);

                            _receiveState = ExtensionFrameState.Payload;
                            ++offset;
                            --bytesTransferred;
                        }
                        else
                        {
                            var isBufferCopyCompleted = CopyToReceiveBuffer(buffer, ref offset, ref bytesTransferred);
                            if (isBufferCopyCompleted)
                            {
                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(_receiveBuffer, 0, 4);

                                SetReceivePayloadLength(BitConverter.ToInt32(_receiveBuffer, 0));
                            }
                        }
                        break;

                    case ExtensionFrameState.Payload:
                        return ParsePayload(buffer, ref offset, ref bytesTransferred);
                }
            }

            return false;
        }

        /// <summary>
        ///     Connection have been lost. Reset state and return buffers.
        /// </summary>
        /// <param name="context"></param>
        public void ResetWrite(WriterContext context)
        {
            if (_sendBuffer != WriterContext.EmptySegment)
            {
                context.ReturnBuffer(_sendBuffer);
                _sendBuffer = WriterContext.EmptySegment;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="context">Used to enqueue bytes for delivery.</param>
        /// <returns><c>true</c> if more buffers can be appened.</returns>
        public bool Write(WriterContext context)
        {
            //_sendCompleted = true when previous send is done. Init everything for this send.
            if (_sendBuffer == WriterContext.EmptySegment)
                _sendBuffer = context.DequeueBuffer();
            var offset = _sendBuffer.Offset;
            if (_sendCompleted)
            {
                var buffer = _sendBuffer.Array;
                var len = GetContentLength();
                if (len > 512)
                    _flags = _flags | FrameFlags.LargeFrame;

                buffer[offset++] = (byte) _flags;
                buffer[offset++] = _extensionId;
                if (len > 512)
                {
                    var lenBuffer = BitConverter.GetBytes(len);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(lenBuffer);
                    buffer[offset++] = lenBuffer[0];
                    buffer[offset++] = lenBuffer[1];
                    buffer[offset++] = lenBuffer[2];
                    buffer[offset++] = lenBuffer[3];
                }
                else
                {
                    buffer[offset++] = (byte) len;
                }

                var bytesLeft = _sendBuffer.Count - (offset - _sendBuffer.Offset);
                _sendCompleted = WritePayload(buffer, ref offset, bytesLeft);
            }
            else
            {
                var buffer = _sendBuffer.Array;
                _sendCompleted = WritePayload(buffer, ref offset, _sendBuffer.Count);
            }


            var bytesWritten = offset - _sendBuffer.Offset;
            context.Enqueue(_sendBuffer.Array, _sendBuffer.Offset, bytesWritten);
            return _sendCompleted;
        }

        protected abstract void SetReceivePayloadLength(int length);

        protected abstract int GetContentLength();
        protected abstract bool ParsePayload(byte[] buffer, ref int offset, ref int bytesTransferred);
        protected abstract bool WritePayload(byte[] buffer, ref int offset, int count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CopyToReceiveBuffer(byte[] buffer, ref int offset, ref int bytesTransferred)
        {
            var bytesToCopy = Math.Min(_receiveStateBufferLeft, bytesTransferred);
            Buffer.BlockCopy(buffer, offset, _receiveBuffer, _receiveBufferOffset, bytesToCopy);
            _receiveStateBufferLeft -= bytesToCopy;
            bytesTransferred -= bytesToCopy;
            offset += bytesToCopy;

            if (_receiveStateBufferLeft == 0)
            {
                _receiveBufferOffset = 0;
                return true;
            }


            _receiveBufferOffset += bytesToCopy;
            return false;
        }
    }
}