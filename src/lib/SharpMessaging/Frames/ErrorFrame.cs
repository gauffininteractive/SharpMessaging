using System;
using System.Text;
using SharpMessaging.Connection;

namespace SharpMessaging.Frames
{
    internal class ErrorFrame : IFrame
    {
        private readonly byte[] _readBuffer = new byte[2000];
        private FrameFlags _flags;
        private int _messageLength;
        private int _readOffset;
        private ArraySegment<byte> _readPayloadBuffer;
        private int _receiveBytesLeft;
        private ErrorFrameState _state;
        private int _stateLength;

        public ErrorFrame(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public ErrorFrame()
        {
        }

        public string ErrorMessage { get; private set; }

        public void ResetWrite(WriterContext context)
        {
        }

        public bool Write(WriterContext context)
        {
            var buf = new byte[65535];
            var contentLength = Encoding.UTF8.GetByteCount(ErrorMessage);
            var offset = 2;
            if (contentLength <= 512)
            {
                buf[0] = (byte)FrameFlags.ErrorFrame;
                buf[1] = (byte)contentLength;
            }
            else
            {
                buf[0] = (byte)(FrameFlags.ErrorFrame | FrameFlags.LargeFrame);
                var lenBuf = BitConverter.GetBytes(contentLength);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lenBuf);
                buf[1] = lenBuf[0];
                buf[2] = lenBuf[1];
                buf[3] = lenBuf[2];
                buf[4] = lenBuf[3];
                offset = 5;
            }

            Encoding.UTF8.GetBytes(ErrorMessage, 0, ErrorMessage.Length, buf, offset);
            context.Enqueue(buf, 0, offset + contentLength);
            return true;
        }

        public void ResetRead()
        {
            _receiveBytesLeft = -1;
            _stateLength = -1;
            _state = ErrorFrameState.Flags;
            _readPayloadBuffer = WriterContext.EmptySegment;
            _readOffset = 0;
        }

        public bool Read(byte[] buffer, ref int offset, ref int bytesToProcess)
        {
            var numberOfBytesTransferredFromStart = bytesToProcess;
            var allCompleted = false;
            while (bytesToProcess > 0 && !allCompleted)
            {
                bool isBufferCopyCompleted;
                switch (_state)
                {
                    case ErrorFrameState.Flags:
                        if (buffer[offset] == 32)
                        {
                            throw new BackTrackException("", offset);
                        }

                        _flags = (FrameFlags)buffer[offset];
                        _state = ErrorFrameState.Length;
                        ++offset;
                        --bytesToProcess;
                        _receiveBytesLeft = 4;
                        break;
                    case ErrorFrameState.Length:
                        if ((_flags & FrameFlags.LargeFrame) == 0)
                        {
                            _messageLength = buffer[offset];
                            ++offset;
                            --bytesToProcess;
                        }
                        else
                        {
                            isBufferCopyCompleted = CopyToReadBuffer(buffer, ref offset, ref bytesToProcess);
                            if (isBufferCopyCompleted)
                            {
                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(_readBuffer, 0, 4);
                                _messageLength = BitConverter.ToInt32(_readBuffer, 0);
                            }
                        }
                        _receiveBytesLeft = _messageLength;
                        _state = ErrorFrameState.Message;
                        break;
                    case ErrorFrameState.Message:
                        isBufferCopyCompleted = CopyToReadBuffer(buffer, ref offset, ref bytesToProcess);
                        if (isBufferCopyCompleted)
                        {
                            ErrorMessage = Encoding.UTF8.GetString(_readBuffer, 0, _messageLength);
                            allCompleted = true;
                        }
                        break;
                }
            }
            if (offset < 0)
                throw new InvalidOperationException();

            return allCompleted;
        }

        public void WriteCompleted(WriterContext context)
        {
        }

        private bool CopyToReadBuffer(byte[] buffer, ref int offset, ref int bytesTransferred)
        {
            var bytesToCopy = Math.Min(_receiveBytesLeft, bytesTransferred);
            if (offset + bytesToCopy > buffer.Length)
                throw new ArgumentOutOfRangeException("offset", offset,
                    "Too large: " + offset + "+" + bytesToCopy + "<" + buffer.Length);
            if (_readOffset + bytesToCopy > _readBuffer.Length)
                throw new ArgumentOutOfRangeException("offset", offset,
                    "Too large for state buffer: " + _readOffset + "+" + bytesToCopy + "<" + _readBuffer.Length);

            Buffer.BlockCopy(buffer, offset, _readBuffer, _readOffset, bytesToCopy);
            _receiveBytesLeft -= bytesToCopy;
            bytesTransferred -= bytesToCopy;
            offset += bytesToCopy;

            if (_receiveBytesLeft == 0)
            {
                _readOffset = 0;
                return true;
            }


            _readOffset += bytesToCopy;
            return false;
        }

        private enum ErrorFrameState
        {
            Flags,
            Length,
            Message
        }
    }
}