using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharpMessaging.Persistence
{
    /// <summary>
    ///     Takes care of reading data from a file
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Keeps a file stream open at all time to reduce the number of IO/OS operations required to reading.
    ///     </para>
    ///     <para>
    ///         The file position can only be moved forward to increase perfomance. We have to keep all peeked records in an
    ///         memory internal list because of that. It's therefore important
    ///         that you just don't peek but also Deuque messages once you've received some from peek.
    ///     </para>
    ///     <para>
    ///         This class also creates a class with the file extension <c>.position</c> to keep track of the last record that
    ///         was written. In that way we can keep reading from the
    ///         right position even if the application or OS is restarted/crashed. The position file is append-only, i.e. we
    ///         keep writing the new read positions in the end of the file
    ///         instead of writing over the current position.
    ///     </para>
    /// </remarks>
    public class PersistantQueueFileReader : IPersistantQueueFileReader
    {
        private readonly string _fileName;
        private readonly LinkedList<PeekRecord> _peekedRecords = new LinkedList<PeekRecord>();
        private readonly string _positionFile;
        private readonly QueueRecordSerializer _queueRecordSerializer = new QueueRecordSerializer();
        private byte[] _filePositionToStore;
        private FileStream _positionStream;
        private FileStream _readStream;

        public PersistantQueueFileReader(string fileName)
        {
            _fileName = fileName;
            _positionFile = Path.Combine(Path.GetDirectoryName(fileName),
                Path.GetFileNameWithoutExtension(fileName) + ".position");
        }

        /// <summary>
        ///     current size of the file
        /// </summary>
        public long FileSize
        {
            get { return _readStream.Length; }
        }

        /// <summary>
        ///     Close files
        /// </summary>
        public void Close()
        {
            _readStream.Close();
            _positionStream.Close();
        }

        /// <summary>
        ///     Close and delete files
        /// </summary>
        public void Delete()
        {
            _readStream.Close();
            _positionStream.Close();
            File.Delete(_fileName);
            File.Delete(_positionFile);
        }

        /// <summary>
        ///     Dequeue a set of records.
        /// </summary>
        /// <param name="messages">Will be cleared and then filled with all available buffers</param>
        /// <param name="maxNumberOfMessages">Number of wanted records (will return less if less are available)</param>
        public int Dequeue(List<byte[]> messages, int maxNumberOfMessages)
        {
            int dequeueCount = 0;
            var initialCount = messages.Count;
            byte[] record;
            do
            {
                if (!TryDequeueInternal(out record))
                {
                    if (initialCount != messages.Count)
                        WritePosition();
                    return dequeueCount;
                }


                --maxNumberOfMessages;
                ++dequeueCount;
                messages.Add(record);
            } while (record != null && maxNumberOfMessages > 0);

            if (initialCount != messages.Count)
                WritePosition();

            return dequeueCount;
        }

        /// <summary>
        ///     Open file and move to the correct position (with the help of the position file)
        /// </summary>
        public void Open()
        {
            _readStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096);

            if (!File.Exists(_positionFile))
                _positionStream = new FileStream(_positionFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read,
                    4096,
                    FileOptions.SequentialScan);
            else
            {
                _positionStream = new FileStream(_positionFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read,
                    4096, FileOptions.SequentialScan);
                if (_positionStream.Length > 0)
                {
                    _positionStream.Position = _positionStream.Length - 4;

                    var buffer = new byte[4];
                    var read = _positionStream.Read(buffer, 0, 4);
                    if (read != 4)
                        throw new InvalidOperationException("Position file is corrupt. Delete it");
                    _readStream.Position = BitConverter.ToInt32(buffer, 0);
                }
            }
        }

        /// <summary>
        ///     Read from the file, but do not update the positition (in the position file)
        /// </summary>
        /// <param name="messages">Appended with all available buffers (at most <c>maxNumberOfMessages</c>)</param>
        /// <param name="maxNumberOfMessages">Number of wanted records (will return less if less are available)</param>
        /// <remarks>
        ///     <para>
        ///         Caches peeked records and returns the same if no <c>Dequeue()</c> have been made between the <c>Peek()</c> invocations.
        ///     </para>
        /// </remarks>
        public void Peek(List<byte[]> messages, int maxNumberOfMessages)
        {
            if (_peekedRecords.Any())
            {
                var node = _peekedRecords.First;
                while (messages.Count < maxNumberOfMessages && node != null)
                {
                    var peekRecord = node.Value;
                    messages.Add(peekRecord.Buffer);
                    node = node.Next;
                }
                if (messages.Count == maxNumberOfMessages)
                    return;
            }


            do
            {
                var position = _readStream.Position;
                var record = _queueRecordSerializer.Read(_readStream);
                if (record == null)
                    return;

                _peekedRecords.AddLast(new PeekRecord(position, record.Length, record));

                --maxNumberOfMessages;
                messages.Add(record);
            } while (maxNumberOfMessages > 0);
        }

        /// <summary>
        ///     We've failed to read a valid record. Attempt to find the next one.
        /// </summary>
        public void Recover()
        {
            var stx = _readStream.ReadByte();
            while (stx != -1 && stx != 2)
            {
                stx = _readStream.ReadByte();
            }
        }

        /// <summary>
        ///     Try dequeue a buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public bool TryDequeue(out byte[] buffer)
        {
            var item = TryDequeue(out buffer);
            if (item)
                WritePosition();
            return item;
        }

        /// <summary>
        ///     Try to peek at a record
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns><c>true</c> if a record is available; otherwise false.</returns>
        public bool TryPeek(out byte[] buffer)
        {
            if (_peekedRecords.Any())
            {
                buffer = _peekedRecords.First.Value.Buffer;
                return true;
            }

            var position = _readStream.Position;

            buffer = _queueRecordSerializer.Read(_readStream);
            if (buffer == null)
                return false;

            _peekedRecords.AddLast(new PeekRecord(position, buffer.Length, buffer));
            return true;
        }

        private void WritePosition()
        {
            if (_filePositionToStore == null)
                throw new InvalidOperationException("Position already written");

            _positionStream.Write(_filePositionToStore, 0, 4);
            _positionStream.Flush();
            _filePositionToStore = null;
        }

        /// <summary>
        ///     Dequeue and flush current position to file
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private bool TryDequeueInternal(out byte[] buffer)
        {
            if (_peekedRecords.Any())
            {
                var record = _peekedRecords.First();
                _peekedRecords.RemoveFirst();

                //5 = header
                _filePositionToStore = BitConverter.GetBytes(record.Position + record.RecordSize + 5);
                buffer = record.Buffer;
                return true;
            }

            buffer = _queueRecordSerializer.Read(_readStream);
            if (buffer == null)
                return false;

            _filePositionToStore = BitConverter.GetBytes((int) _readStream.Position);
            return true;
        }
    }
}