using System;
using System.Threading;

namespace SharpMessaging.Connection
{
    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>
    ///     Designed so that one thread can do the enqueing and another thread can to the dequeuing. That's at least my
    ///     intention.
    /// </remarks>
    public class CircularQueueList<T>
    {
        private readonly int _capacity;
        private readonly T[] _items;
        private int _count = 0;
        private long _readIndex = 0;
        private long _writeIndex;

        public CircularQueueList(int capacity)
        {
            _capacity = capacity;
            _items = new T[capacity];
        }

        public int Count
        {
            get { return _count; }
        }

        public bool CanResize { get; set; }

        public int Capacity
        {
            get { return _capacity; }
        }


        public T this[int index]
        {
            get
            {
                var realIndex = _readIndex + index;
                if (realIndex >= _items.Length)
                    realIndex -= _items.Length;
                return _items[realIndex];
            }
        }

        public T Dequeue()
        {
            if (_count == 0)
                throw new InvalidOperationException("Queue is empty");

            var ourIndex = _readIndex++;
            if (_readIndex == _items.Length)
                _readIndex = 0;
            var item = _items[ourIndex];
            _items[ourIndex] = default(T);

            // Must be done before after the reading
            // so that the write side doesn't assign a new entry before
            // we have read it.
            Interlocked.Decrement(ref _count);


            return item;
        }

        public void Enqueue(T item)
        {
            if (_count >= _capacity)
                throw new InvalidOperationException("Queue is full");

            _items[_writeIndex++] = item;

            //must be done after the assignment so that the read thread
            //doesnt read the entry before it has been assigned.
            Interlocked.Increment(ref _count);

            if (_writeIndex == _items.Length)
                _writeIndex = 0;
        }

        public T Peek()
        {
            return this[0];
        }

        public void Clear()
        {
            while (Count>0)
            {
                Dequeue();
            }
            _writeIndex = 0;
            _readIndex = 0;
            _count = 0;
            
        }
    }
}