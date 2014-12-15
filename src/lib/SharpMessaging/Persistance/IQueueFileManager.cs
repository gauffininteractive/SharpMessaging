using System;

namespace SharpMessaging.Persistance
{
    /// <summary>
    ///     Used to keep track of all files that have been created for a queue.
    /// </summary>
    public interface IQueueFileManager
    {
        /// <summary>
        ///     Number of items in our queue files when we scanned all files.
        /// </summary>
        int InitialQueueLength { get; }

        /// <summary>
        ///     Scan through the file system after all files for the queue
        /// </summary>
        void Scan();

        /// <summary>
        ///     Check if we can move to next read file
        /// </summary>
        /// <returns><c>true</c> if there is another file; otherwise false.</returns>
        bool CanIncreaseReadFile();

        /// <summary>
        ///     Open next file to read from
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">CanIncreaseReadFile says no</exception>
        IPersistantQueueFileReader OpenNextReadFile();

        /// <summary>
        ///     Open file that we should continue to write to.
        /// </summary>
        /// <returns></returns>
        IPersistantQueueFileWriter OpenCurrentWriteFile();

        /// <summary>
        ///     Open file that we should start to read from.
        /// </summary>
        /// <returns></returns>
        IPersistantQueueFileReader OpenCurrentReadFile();

        /// <summary>
        ///     Current write file is full, open a new one.
        /// </summary>
        /// <returns></returns>
        IPersistantQueueFileWriter CreateNewWriteFile();
    }
}