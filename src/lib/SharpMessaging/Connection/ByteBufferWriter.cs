using System;

namespace SharpMessaging.Connection
{
    public sealed class ByteBufferWriter : IBufferWriter
    {
        private readonly byte[] _buffer;
        private int _bytesLeft;
        private int _offset;

        public ByteBufferWriter(byte[] buffer, int offset, int count)
        {
            _buffer = buffer;
            _offset = offset;
            _bytesLeft = count;
        }

        /// <summary>
        ///     Connection have been lost. Reset state and return buffers.
        /// </summary>
        /// <param name="context"></param>
        public void ResetWrite(WriterContext context)
        {
        }

        public bool Write(WriterContext context)
        {
            var bytesToCopy = Math.Min(context.BytesLeftToEnqueue, _bytesLeft);
            context.Enqueue(new ArraySegment<byte>(_buffer, _offset, bytesToCopy));
            _offset += bytesToCopy;
            _bytesLeft -= bytesToCopy;
            return _bytesLeft == 0;
        }

        public void WriteCompleted(WriterContext context)
        {
        }
    }
}