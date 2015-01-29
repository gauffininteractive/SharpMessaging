using System.Collections.Generic;

namespace SharpMessaging.Persistence
{
    /// <summary>
    ///     A queue which stores entries on a persistant storage medium
    /// </summary>
    public interface IPersistantQueue
    {
        /// <summary>
        ///     Max amount of data in each file
        /// </summary>
        int MaxFileSizeInBytes { get; set; }

        /// <summary>
        ///     Current amount of queued items.
        /// </summary>
        int Count { get; }

        /// <summary>
        ///     Close file
        /// </summary>
        void Close();

        /// <summary>
        ///     Dequeue a set of records.
        /// </summary>
        /// <param name="buffers">Will be cleared and then filled with all available buffers</param>
        /// <param name="maxAmountOfMessages">Number of wanted records (will return less if less are available)</param>
        void Dequeue(List<byte[]> buffers, int maxAmountOfMessages);

        /// <summary>
        ///     Write a record to the file
        /// </summary>
        /// <param name="buffer"></param>
        /// <remarks>
        ///     <para>
        ///         Make sure that you call <see cref="FlushWriter" /> once you've enqueued all messages in the current batch.
        ///         There is otherwise no guarantee that the content have been written to disk.
        ///     </para>
        /// </remarks>
        void Enqueue(byte[] buffer);

        /// <summary>
        ///     Write a record to the file
        /// </summary>
        /// <param name="buffer">Buffer to read record from</param>
        /// <param name="offset">Start offset of the record</param>
        /// <param name="count">Number of bytes to write (starting at offset)</param>
        /// <remarks>
        ///     <para>
        ///         Make sure that you call <see cref="FlushWriter" /> once you've enqueued all messages in the current batch.
        ///         There is otherwise no guarantee that the content have been written to disk.
        ///     </para>
        /// </remarks>
        void Enqueue(byte[] buffer, int offset, int count);

        /// <summary>
        ///     Flush write IO to disk, i.e. make sure that everything is written to the file (and not being left in the OS IO
        ///     buffer)
        /// </summary>
        /// <remarks>
        ///     <para>MUST be done after enqueue operations.</para>
        /// </remarks>
        void FlushWriter();

        /// <summary>
        ///     Open our files
        /// </summary>
        void Open();

        /// <summary>
        ///     Read from the file, but do not update the positition (in the position file)
        /// </summary>
        /// <param name="buffers">Will be cleared and then filled with all available buffers</param>
        /// <param name="maxNumberOfMessages">Number of wanted records (will return less if less are available)</param>
        void Peek(List<byte[]> buffers, int maxNumberOfMessages);

        /// <summary>
        ///     Try to dequeue a record
        /// </summary>
        /// <param name="buffer">Record dequeued</param>
        /// <returns><c>true</c> if there was a record available; otherwise <c>false</c></returns>
        bool TryDequeue(out byte[] buffer);

        /// <summary>
        ///     Try to peek a record (i.e. to not update the position file)
        /// </summary>
        /// <param name="buffer">Record dequeued</param>
        /// <returns><c>true</c> if there was a record available; otherwise <c>false</c></returns>
        bool TryPeek(out byte[] buffer);

        /// <summary>
        /// Remove messages directly without reading them.
        /// </summary>
        /// <param name="count"></param>
        void Remove(int count);
    }
}