using System.Collections.Generic;

namespace SharpMessaging.Persistence
{
    public interface IQueueStorage
    {
        /// <summary>
        ///     Number of messages in the queue
        /// </summary>
        int Count { get; }

        void Enqueue(object message);
        void Enqueue(IEnumerable<object> messages);
        void Peek(IList<object> messages, int maxNumberOfMessages);

        /// <summary>
        ///     Remove messages from the queue
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="maxNumberOfMessages"></param>
        void Dequeue(IList<object> messages, int maxNumberOfMessages);

        /// <summary>
        /// We want to remove the specified count from the file without reading them.
        /// </summary>
        /// <param name="ackCount">Number of items to dequeue</param>
        void Remove(int ackCount);
    }
}