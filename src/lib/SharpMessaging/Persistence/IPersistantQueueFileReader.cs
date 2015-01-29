using System.Collections.Generic;

namespace SharpMessaging.Persistence
{
    public interface IPersistantQueueFileReader
    {
        /// <summary>
        ///     current size of the file
        /// </summary>
        long FileSize { get; }

        /// <summary>
        ///     Close files
        /// </summary>
        void Close();

        /// <summary>
        ///     Close and delete files
        /// </summary>
        void Delete();

        /// <summary>
        ///     Dequeue a set of records.
        /// </summary>
        /// <param name="messages">Will be cleared and then filled with all available buffers</param>
        /// <param name="maxNumberOfMessages">Number of wanted records (will return less if less are available)</param>
        /// <returns>Amount of items that was dequeued.</returns>
        int Dequeue(List<byte[]> messages, int maxNumberOfMessages);

        /// <summary>
        ///     Open file and move to the correct position (with the help of the position file)
        /// </summary>
        void Open();

        /// <summary>
        ///     Read from the file, but do not update the positition (in the position file)
        /// </summary>
        /// <param name="messages">Will be cleared and then filled with all available buffers</param>
        /// <param name="maxNumberOfMessages">Number of wanted records (will return less if less are available)</param>
        /// <remarks>
        ///     <para>
        ///         Caches peeked records and returns the same if no Dequeus have been made between the Peeks
        ///     </para>
        /// </remarks>
        void Peek(List<byte[]> messages, int maxNumberOfMessages);

        /// <summary>
        ///     We've failed to read a valid record. Attempt to find the next one.
        /// </summary>
        void Recover();

        /// <summary>
        ///     Try dequeue a buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        bool TryDequeue(out byte[] buffer);

        /// <summary>
        ///     Try to peek at a record
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns><c>true</c> if a record is available; otherwise false.</returns>
        bool TryPeek(out byte[] buffer);
    }
}