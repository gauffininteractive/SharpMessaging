using System;
using System.IO;

namespace SharpMessaging.Persistance
{
    internal class QueueRecordSerializer
    {
        private readonly byte[] _readSizeBuffer = new byte[4];
        private byte[] _writeBuffer = new byte[65535];

        public void Serialize(Stream destination, byte[] data, int offset, int length)
        {
            var buffer = BitConverter.GetBytes(length);
            if (_writeBuffer.Length < length + 4)
                _writeBuffer = new byte[length*2 + 4];

            _writeBuffer[0] = 2; //STX
            Buffer.BlockCopy(buffer, 0, _writeBuffer, 1, 4);
            Buffer.BlockCopy(data, 0, _writeBuffer, 5, length);
            destination.Write(_writeBuffer, 0, length + 5);
        }

        public byte[] Read(Stream sourceStream)
        {
            var stx = sourceStream.ReadByte();
            if (stx == -1)
                return null; // EOF

            if (stx != 2)
                throw new InvalidDataException("Failed to find STX at the current position");

            var read = sourceStream.Read(_readSizeBuffer, 0, 4);
            if (read == 0)
                return null;

            if (read != 4)
                throw new InvalidDataException(
                    string.Format(
                        "Expected to get four bytes of data (record length), but got just {0} bytes at position {1}. File is corrupt.",
                        read, (sourceStream.Position - read)));

            var recordLength = BitConverter.ToInt32(_readSizeBuffer, 0);

            // this can either be a corrupt file (if other than the write file) or a record that the write class has not yet completed.
            // **commented out until we can identify the write file in a thread safe fashion. The file should not be EOF currently as we use a lock at a higher level**
            //if (recordLength + _readStream.Position > _readStream.Length)
            //    return null;

            var buffer = new byte[recordLength];
            read = sourceStream.Read(buffer, 0, recordLength);
            if (read != recordLength)
                throw new InvalidDataException(
                    string.Format(
                        "Expected to get " + recordLength +
                        " bytes of data (data record), but got just {0} bytes at position {1}. File is corrupt.",
                        read, (sourceStream.Position - read)));

            return buffer;
        }
    }
}