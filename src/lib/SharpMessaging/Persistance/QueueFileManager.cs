using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharpMessaging.Persistance
{
    /// <summary>
    ///     Used to locate the correct file for reading and writing
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Supports index wrapping (i.e.  the write index may wrap and start at 0 again, while the read index is a
    ///         higher number)
    ///     </para>
    ///     <para>Takes for granted that a position file only is created for the read file</para>
    /// </remarks>
    public class QueueFileManager : IQueueFileManager
    {
        private readonly string _optionalReadQueuePath;
        private readonly string _queueName;
        private readonly string _queuePath;
        private readonly LinkedList<string> _files = new LinkedList<string>();

        public QueueFileManager(string queuePath, string optionalReadQueuePath, string queueName)
        {
            if (queuePath == null) throw new ArgumentNullException("queuePath");
            if (optionalReadQueuePath == null) throw new ArgumentNullException("optionalReadQueuePath");
            if (queueName == null) throw new ArgumentNullException("queueName");

            _optionalReadQueuePath = optionalReadQueuePath;
            _queuePath = queuePath;
            _queueName = queueName;
        }

        private string WriteFileName { get; set; }
        private string ReadFileName { get; set; }

        /// <summary>
        ///     Number of items in our queue files when we scanned all files.
        /// </summary>
        public int InitialQueueLength { get; set; }

        /// <summary>
        ///     Scan directory for all files used by this queue.
        /// </summary>
        /// <remarks>
        ///     <para>Will also set the correct read and file queue.</para>
        /// </remarks>
        public void Scan()
        {
            var scannedFiles = Directory.Exists(_optionalReadQueuePath)
                ? Directory.GetFiles(_optionalReadQueuePath, _queueName + "*.dat")
                    .OrderBy(File.GetLastWriteTimeUtc)
                    .ToList()
                : new List<string>();

         
            if (scannedFiles.Count == 0)
            {
                WriteFileName = GetFileName();
                ReadFileName = WriteFileName;
                _files.AddLast(WriteFileName);
            }
            else
            {
                WriteFileName = scannedFiles.Last();
                ReadFileName = scannedFiles.First();
                foreach (var file in scannedFiles)
                {
                    _files.AddLast(file);
                }
            }

            InitialQueueLength = CalculateQueueLength();
        }

        /// <summary>
        ///     There are more files and we can therefore move to the next one
        /// </summary>
        /// <returns></returns>
        public bool CanIncreaseReadFile()
        {
            return _files.Count > 1;
        }

        /// <summary>
        ///     Open next read file
        /// </summary>
        /// <returns></returns>
        public IPersistantQueueFileReader OpenNextReadFile()
        {
            if (!CanIncreaseReadFile())
                throw new InvalidOperationException("There is only one file. We cannot move forward.");

            _files.RemoveFirst();

            ReadFileName = _files.First.Value;
            var file = new PersistantQueueFileReader(ReadFileName);
            file.Open();
            return file;
        }

        public IPersistantQueueFileWriter OpenCurrentWriteFile()
        {
            var file = new PersistantQueueFileWriter(WriteFileName);
            file.Open();
            return file;
        }

        public IPersistantQueueFileReader OpenCurrentReadFile()
        {
            var file = new PersistantQueueFileReader(ReadFileName);
            file.Open();
            return file;
        }

        /// <summary>
        ///     Move to the next file.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         TODO: Delete the oldest file if the number of files have been exceeded.
        ///     </para>
        /// </remarks>
        public IPersistantQueueFileWriter CreateNewWriteFile()
        {
            WriteFileName = GetFileName();
            _files.AddLast(WriteFileName);
            var file = new PersistantQueueFileWriter(WriteFileName);
            file.Open();
            return file;
        }

        private int CalculateQueueLength()
        {
            var queueLength = 0;
            var serializer = new QueueRecordSerializer();
            foreach (var file in _files)
            {
                var startPosition = 0;
                var positionFile = file.Replace(".dat", ".position");
                if (FileExists(positionFile))
                {
                    using (
                        var stream = new FileStream(positionFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                            16384, FileOptions.SequentialScan))
                    {
                        if (stream.Length > 4)
                        {
                            stream.Position = stream.Length - 4;
                            var intBuffer = new byte[4];
                            var pos = stream.Read(intBuffer, 0, 4);
                            startPosition = BitConverter.ToInt32(intBuffer, 0);
                        }
                    }
                }

                using (
                    var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 16384,
                        FileOptions.SequentialScan))
                {
                    stream.Position = startPosition;
                    while (true)
                    {
                        var record = serializer.Read(stream);
                        if (record != null)
                            ++queueLength;
                        else
                            break;
                    }
                }
            }

            return queueLength;
        }

        /// <summary>
        ///     Extract index from file name
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static int GetFileIndex(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException("fileName");

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var pos = nameWithoutExtension.LastIndexOf('_');
            if (pos == -1)
                throw new FormatException("File is not a queue file: " + fileName);

            return int.Parse(nameWithoutExtension.Substring(pos + 1));
        }

        protected virtual bool FileExists(string fileName)
        {
            return File.Exists(fileName);
        }

        private string GetFileName()
        {
            return Path.Combine(_queuePath, string.Format("{0}_{1}.dat", _queueName, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")));
        }
    }
}