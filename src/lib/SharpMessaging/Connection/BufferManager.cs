using System;
using System.Collections.Generic;

namespace SharpMessaging.Connection
{
    /// <summary>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Not thread-safe.
    ///     </para>
    /// </remarks>
    public class BufferManager
    {
        private readonly int _bufferSize = 65535;
        private readonly byte[] _largeBuffer;
        private readonly Queue<int> _offsets = new Queue<int>();

        public BufferManager(int bufferSize, int numberOfBuffers)
        {
            _bufferSize = bufferSize;
            _largeBuffer = new byte[_bufferSize*numberOfBuffers];
            for (var i = 0; i < numberOfBuffers; i++)
            {
                _offsets.Enqueue(i*bufferSize);
            }
        }

        public ArraySegment<byte> Dequeue()
        {
            if (_offsets.Count == 0)
                throw new InvalidOperationException("Someone stole our buffer");

            var offset = _offsets.Dequeue();
            return new ArraySegment<byte>(_largeBuffer, offset, _bufferSize);
        }

        public void Enqueue(ArraySegment<byte> buffer)
        {
            if (buffer.Array != _largeBuffer)
                throw new ArgumentException("Buffer did not come from this pool", "buffer");

            _offsets.Enqueue(buffer.Offset);
        }
    }
}