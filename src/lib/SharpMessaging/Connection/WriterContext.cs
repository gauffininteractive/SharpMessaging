using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace SharpMessaging.Connection
{
    public sealed class WriterContext
    {
        public static readonly ArraySegment<byte> EmptySegment = new ArraySegment<byte>(new byte[0]);
        public static int MaxCountPerOperation = 10000000;
        private readonly BufferManager _bufferManager;
        private readonly List<SendPacketsElement> _buffers = new List<SendPacketsElement>(1000);
        private int _bytesLeft = MaxCountPerOperation;

        public WriterContext(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
        }

        public int BytesLeftToEnqueue
        {
            get { return _bytesLeft; }
            internal set { _bytesLeft = value; }
        }

        public bool IsPartial { get; set; }

        public ArraySegment<byte> DequeueBuffer()
        {
            return _bufferManager.Dequeue();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(ArraySegment<byte> buffer)
        {
            if (_bytesLeft <= 0)
                throw new InvalidOperationException(
                    "Too much data, check the BytesLeftToEnqueue property before enqueing too much.");
            _bytesLeft -= buffer.Count;
            _buffers.Add(new SendPacketsElement(buffer.Array, buffer.Offset, buffer.Count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(SendPacketsElement buffer)
        {
            if (_bytesLeft <= 0)
                throw new InvalidOperationException(
                    "Too much data, check the BytesLeftToEnqueue property before enqueing too much.");
            _bytesLeft -= buffer.Count;
            _buffers.Add(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(byte[] buffer, int offset, int count)
        {
            if (_bytesLeft <= 0)
                throw new InvalidOperationException(
                    "Too much data, check the BytesLeftToEnqueue property before enqueing too much.");
            _bytesLeft -= count;
            _buffers.Add(new SendPacketsElement(buffer, offset, count));
        }

        public List<SendPacketsElement> GetPackets()
        {
            return _buffers;
        }

        public void Reset()
        {
            _buffers.Clear();
            _bytesLeft = MaxCountPerOperation;
        }

        public void ReturnBuffer(ArraySegment<byte> buffer)
        {
            _bufferManager.Enqueue(buffer);
        }
    }
}