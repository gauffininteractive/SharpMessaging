using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SharpMessaging.Persistence
{
    /// <summary>
    /// </summary>
    /// <remarks>
    ///     We use locks quite sloppy now. In real life it's only required when reads and writes are done in the same file and
    ///     that the read is on the last record in the file.
    /// </remarks>
    /// TODO: Catch InvalidDataException and call _readFile.Recover() in all read operations
    public class QueueService : IQueueStorage
    {
        private readonly ManualResetEvent _dataEnqueuedEvent = new ManualResetEvent(false);
        private readonly IQueueItemSerializer _itemSerializer;
        private readonly IPersistantQueue _queue;
        private readonly object _syncLock = new object();

        public QueueService(string queueDirectory, string optionalReadQueueDirectory, string queueName,
            IQueueItemSerializer itemSerializer)
        {
            _itemSerializer = itemSerializer;
            if (!Directory.Exists(queueDirectory))
                Directory.CreateDirectory(queueDirectory);

            _queue = new PersistantQueue(queueDirectory, optionalReadQueueDirectory, queueName);
            _queue.Open();
        }

        public QueueService(IPersistantQueue persistantQueue, IQueueItemSerializer itemSerializer)
        {
            if (persistantQueue == null) throw new ArgumentNullException("persistantQueue");
            _queue = persistantQueue;
            _itemSerializer = itemSerializer;
            _queue.Open();
        }

        /// <summary>
        ///     Number of messages in the queue
        /// </summary>
        public int Count
        {
            get { return _queue.Count; }
        }


        /// <summary>
        ///     Close queue, but do not remove anything in it or the queue itself.
        /// </summary>
        public void Close()
        {
            _queue.Close();
        }

        public void Enqueue(object message)
        {
            var buffer = _itemSerializer.Serialize(message);

            lock (_syncLock)
            {
                _queue.Enqueue(buffer);
                _queue.FlushWriter();
            }

            _dataEnqueuedEvent.Set();
        }

        public void Enqueue(IEnumerable<object> messages)
        {
            lock (_syncLock)
            {
                foreach (var message in messages)
                {
                    var buf = _itemSerializer.Serialize(message);
                    _queue.Enqueue(buf);
                }

                _queue.FlushWriter();
            }
        }

        public void Peek(IList<object> messages, int maxNumberOfMessages)
        {
            var bufferList = new List<byte[]>();
            lock (_syncLock)
            {
                _queue.Peek(bufferList, maxNumberOfMessages);
            }

            // Wait if there are no more data to be delivered.
            if (!bufferList.Any())
            {
                _dataEnqueuedEvent.Reset();
                if (!_dataEnqueuedEvent.WaitOne(100))
                    return;

                lock (_syncLock)
                {
                    _queue.Peek(bufferList, maxNumberOfMessages);
                }
            }

            foreach (var buffer in bufferList)
            {
                var obj = _itemSerializer.Deserialize(buffer);
                messages.Add(obj);
            }
        }

        /// <summary>
        ///     Remove messages from the queue
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="maxNumberOfMessages"></param>
        public void Dequeue(IList<object> messages, int maxNumberOfMessages)
        {
            if (messages == null) throw new ArgumentNullException("messages");
            if (maxNumberOfMessages == 0)
                throw new ArgumentOutOfRangeException("maxNumberOfMessages", maxNumberOfMessages,
                    "Must specify a valid count.");

            lock (_syncLock)
            {
                var bufferList = new List<byte[]>();
                _queue.Dequeue(bufferList, maxNumberOfMessages);
                foreach (var buffer in bufferList)
                {
                    var obj = _itemSerializer.Deserialize(buffer);
                    messages.Add(obj);
                }
            }
        }

        public void Remove(int ackCount)
        {
            lock (_syncLock)
            {
                _queue.Remove(ackCount);
            }
        }
    }
}