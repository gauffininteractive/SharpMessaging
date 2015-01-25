using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpMessaging.Persistence
{
    /// <summary>
    ///     Facade for the queue files
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Persistant coordinates the reader and writer classes. It switches files when required and keep track of the
    ///         file size.
    ///     </para>
    /// </remarks>
    public class PersistantQueue : IPersistantQueue
    {
        private readonly IQueueFileManager _fileManager;
        private IPersistantQueueFileReader _readFile;
        private IPersistantQueueFileWriter _writefile;

        public PersistantQueue(string queuePath, string optionalReadQueuePath, string queueName)
        {
            if (queuePath == null) throw new ArgumentNullException("queuePath");
            if (queueName == null) throw new ArgumentNullException("queueName");

            _fileManager = new QueueFileManager(queuePath, optionalReadQueuePath, queueName);
            MaxFileSizeInBytes = 30000000;
        }

        public PersistantQueue(string queuePath, string queueName)
        {
            if (queuePath == null) throw new ArgumentNullException("queuePath");
            if (queueName == null) throw new ArgumentNullException("queueName");

            _fileManager = new QueueFileManager(queuePath, queueName);
            MaxFileSizeInBytes = 30000000;
        }

        public PersistantQueue(IQueueFileManager queueFileManager)
        {
            if (queueFileManager == null) throw new ArgumentNullException("queueFileManager");

            MaxFileSizeInBytes = 30000000;
            _fileManager = queueFileManager;
        }

        /// <summary>
        ///     Max amount of bytes that can be written to the file.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Do note that the writer will exceed this limit and switch to a new file as soon as it can.
        ///     </para>
        /// </remarks>
        public int MaxFileSizeInBytes { get; set; }

        /// <summary>
        ///     Close both the reader and writer side.
        /// </summary>
        public void Close()
        {
            _readFile.Close();
            _writefile.Close();
        }

        /// <summary>
        ///     Dequeue a set of records.
        /// </summary>
        /// <param name="buffers">Will be cleared and then filled with all available buffers</param>
        /// <param name="maxAmountOfMessages">Number of wanted records (will return less if less are available)</param>
        public void Dequeue(List<byte[]> buffers, int maxAmountOfMessages)
        {
            if (buffers == null) throw new ArgumentNullException("buffers");

            buffers.Clear();
            _readFile.Dequeue(buffers, maxAmountOfMessages);
            if (buffers.Any())
                return;

            if (!TryOpenNextReadFile())
                return;

            _readFile.Dequeue(buffers, maxAmountOfMessages);
        }

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
        public void Enqueue(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");

            Enqueue(buffer, 0, buffer.Length);
        }

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
        public void Enqueue(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");

            if (_writefile.FileSize > MaxFileSizeInBytes)
            {
                _writefile.Close();
                _writefile = _fileManager.CreateNewWriteFile();
            }

            _writefile.Enqueue(buffer, offset, count);
        }

        /// <summary>
        ///     Flush write IO to disk, i.e. make sure that everything is written to the file (and not being left in the OS IO
        ///     buffer)
        /// </summary>
        /// <remarks>
        ///     <para>MUST be done after enqueue operations</para>
        /// </remarks>
        public void FlushWriter()
        {
            _writefile.Flush();
        }

        /// <summary>
        ///     Open our files
        /// </summary>
        public void Open()
        {
            _fileManager.Scan();
            _writefile = _fileManager.OpenCurrentWriteFile();
            _readFile = _fileManager.OpenCurrentReadFile();
        }

        /// <summary>
        ///     Read from the file, but do not update the positition (in the position file)
        /// </summary>
        /// <param name="buffers">Will be cleared and then filled with all available buffers</param>
        /// <param name="maxNumberOfMessages">Number of wanted records (will return less if less are available)</param>
        public void Peek(List<byte[]> buffers, int maxNumberOfMessages)
        {
            if (buffers == null) throw new ArgumentNullException("buffers");

            buffers.Clear();
            _readFile.Peek(buffers, maxNumberOfMessages);
            if (buffers.Any())
                return;

            if (!TryOpenNextReadFile())
                return;

            _readFile.Peek(buffers, maxNumberOfMessages);
        }

        /// <summary>
        ///     Try to dequeue a record
        /// </summary>
        /// <param name="buffer">Record dequeued</param>
        /// <returns><c>true</c> if there was a record available; otherwise <c>false</c></returns>
        public bool TryDequeue(out byte[] buffer)
        {
            if (_readFile.TryDequeue(out buffer))
                return true;

            if (!TryOpenNextReadFile())
                return false;

            return _readFile.TryDequeue(out buffer);
        }

        /// <summary>
        ///     Try to peek a record (i.e. to not update the position file)
        /// </summary>
        /// <param name="buffer">Record dequeued</param>
        /// <returns><c>true</c> if there was a record available; otherwise <c>false</c></returns>
        public bool TryPeek(out byte[] buffer)
        {
            if (_readFile.TryPeek(out buffer))
                return true;

            if (!TryOpenNextReadFile())
                return false;

            return _readFile.TryPeek(out buffer);
        }

        public int GetInitialQueueSize()
        {
            return _fileManager.InitialQueueLength;
        }

        private bool TryOpenNextReadFile()
        {
            // we are in the same file for reads and writes. i.e. it do currently not have any more records to read.
            if (!_fileManager.CanIncreaseReadFile())
                return false;

            _readFile.Delete();
            _readFile = _fileManager.OpenNextReadFile();

            return true;
        }
    }
}