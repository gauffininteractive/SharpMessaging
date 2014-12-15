using System;

namespace SharpMessaging.Extensions.Ack
{
    public class AckFrame : ExtensionFrame
    {
        private readonly byte[] _inboundSequenceNumber = new byte[2];
        private readonly byte[] _outboundSequenceNumber;
        private int _inboundBytesLeft = 2;
        private int _inboundOffset;

        public AckFrame(byte extensionId, ushort sequenceNumber) : base(extensionId)
        {
            SequenceNumber = sequenceNumber;
            _outboundSequenceNumber = BitConverter.GetBytes(sequenceNumber);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(_outboundSequenceNumber);
        }

        /// <summary>
        ///     Message  (all messages up to this one) to ack
        /// </summary>
        public ushort SequenceNumber { get; set; }

        protected override void SetReceivePayloadLength(int length)
        {
        }

        protected override int GetContentLength()
        {
            return 2;
        }

        protected override bool ParsePayload(byte[] buffer, ref int offset, ref int bytesTransferred)
        {
            var toCopy = Math.Min(_inboundBytesLeft, bytesTransferred);
            Buffer.BlockCopy(buffer, offset, _inboundSequenceNumber, _inboundOffset, _inboundBytesLeft);
            bytesTransferred -= toCopy;
            offset += toCopy;
            _inboundBytesLeft -= toCopy;

            if (_inboundBytesLeft == 0)
            {
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(_inboundSequenceNumber);
                SequenceNumber = BitConverter.ToUInt16(_inboundSequenceNumber, 0);
                _inboundOffset = 0;
                _inboundBytesLeft = 2;
                return true;
            }

            return false;
        }

        protected override bool WritePayload(byte[] buffer, ref int offset, int count)
        {
            Buffer.BlockCopy(_outboundSequenceNumber, 0, buffer, offset, 2);
            offset += 2;
            return true;
        }
    }
}