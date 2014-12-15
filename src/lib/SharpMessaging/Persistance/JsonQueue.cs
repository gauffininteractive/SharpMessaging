using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using fastJSON;

namespace SharpMessaging.Persistance
{
    /// <summary>
    /// </summary>
    /// <remarks>
    ///     We use locks quite sloppy now. In real life it's only required when reads and writes are done in the same file and
    ///     that the read is on the last record in the file.
    /// </remarks>
    /// TODO: Catch InvalidDataException and call _readFile.Recover() in all read operations
    public class JsonQueue
    {
        private readonly ManualResetEvent _dataEnqueuedEvent = new ManualResetEvent(false);
        private readonly IPersistantQueue _queue;
        private readonly List<byte[]> _readList = new List<byte[]>();
        private readonly object _syncLock = new object();
        private int _queueCount;

        public JsonQueue(string queueDirectory, string optionalReadQueueDirectory, string queueName)
        {
            if (!Directory.Exists(queueDirectory))
                Directory.CreateDirectory(queueDirectory);

            _queue = new PersistantQueue(queueDirectory, optionalReadQueueDirectory, queueName);
            _queue.Open();
        }

        public JsonQueue(IPersistantQueue persistantQueue)
        {
            if (persistantQueue == null) throw new ArgumentNullException("persistantQueue");
            _queue = persistantQueue;
            _queue.Open();
            _queueCount = _queue.GetInitialQueueSize();
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
            var str = JSON.ToJSON(message);
            _queueCount++;

            lock (_syncLock)
            {
                _queue.Enqueue(Encoding.UTF8.GetBytes(str));
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
                    var str = JSON.ToJSON(message);
                    _queue.Enqueue(Encoding.UTF8.GetBytes(str));
                    ++_queueCount;
                }

                _queue.FlushWriter();
            }
        }

        /// <summary>
        ///     Number of messages in the queue
        /// </summary>
        public int GetQueueCount()
        {
            return _queueCount;
        }

        public void Peek(IList<object> messages, int maxNumberOfMessages)
        {
            lock (_syncLock)
            {
                _readList.Clear();
                _queue.Peek(_readList, maxNumberOfMessages);
            }

            // Wait if there are no more data to be delivered.
            if (!_readList.Any())
            {
                _dataEnqueuedEvent.Reset();
                if (!_dataEnqueuedEvent.WaitOne(100))
                    return;

                lock (_syncLock)
                {
                    _readList.Clear();
                    _queue.Peek(_readList, maxNumberOfMessages);
                }
            }

            foreach (var buffer in _readList)
            {
                var obj = JSON.Parse(Encoding.UTF8.GetString(buffer));
                messages.Add(obj);
            }
        }

        public void PopMessages(int numberOfMessages)
        {
            _readList.Clear();
            if (numberOfMessages == 0)
                return;

            lock (_syncLock)
            {
                _queue.Dequeue(_readList, numberOfMessages);
                _queueCount -= _readList.Count;
            }
        }
    }
}