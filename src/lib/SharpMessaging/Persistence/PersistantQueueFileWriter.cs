using System.IO;

namespace SharpMessaging.Persistence
{
    /// <summary>
    ///     Append only writer to a file.
    /// </summary>
    /// <remarks>
    ///     <para>Writes </para>
    /// </remarks>
    public class PersistantQueueFileWriter : IPersistantQueueFileWriter
    {
        private readonly string _fileName;
        private readonly QueueRecordSerializer _serializer = new QueueRecordSerializer();
        private FileStream _writeStream;

        public PersistantQueueFileWriter(string fileName)
        {
            _fileName = fileName;
        }

        public long FileSize
        {
            get { return _writeStream.Length; }
        }

        public void Enqueue(byte[] data)
        {
            Enqueue(data, 0, data.Length);
        }

        public void Enqueue(byte[] data, int offset, int length)
        {
            _serializer.Serialize(_writeStream, data, offset, length);
        }

        public void Open()
        {
            _writeStream = new FileStream(_fileName, FileMode.Append, FileAccess.Write, FileShare.Read, 4096,
                FileOptions.SequentialScan);
        }

        public void Close()
        {
            _writeStream.Close();
        }

        public void Flush()
        {
            _writeStream.Flush();
        }
    }
}