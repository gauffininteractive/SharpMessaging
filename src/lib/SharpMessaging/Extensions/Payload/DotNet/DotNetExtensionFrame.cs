using System;
using System.Text;

namespace SharpMessaging.Extensions.Payload.DotNet
{
    /// <summary>
    ///     A frame that specifies the .NET type that should be used during deserialization of the next message.
    /// </summary>
    public class DotNetExtensionFrame : ExtensionFrame
    {
        private readonly Type _payloadType;
        private readonly byte[] _receiveBuffer = new byte[512];
        private int _receiveBytesLeft;
        private int _receiveOffset;
        private int _receivePayloadLength;

        public DotNetExtensionFrame(byte extensionId, object dotNetType)
            : base(extensionId)
        {
            _payloadType = (Type) dotNetType;
        }

        public DotNetExtensionFrame(byte extensionId) : base(extensionId)
        {
        }


        public void Reset()
        {
            _receiveBytesLeft = 0;
            _receiveOffset = 0;
        }

        protected override void SetReceivePayloadLength(int length)
        {
            _receiveBytesLeft = length;
            _receivePayloadLength = length;
        }

        protected override int GetContentLength()
        {
            return Encoding.UTF8.GetByteCount(_payloadType.AssemblyQualifiedName);
        }

        protected override bool ParsePayload(byte[] buffer, ref int offset, ref int count)
        {
            // got everything, do not do a temp copy.
            if (_receiveOffset == 0 && count >= _receivePayloadLength)
            {
                Payload = new DotNetType(Encoding.ASCII.GetString(buffer, offset, _receivePayloadLength));
                offset += _receivePayloadLength;
                count -= _receivePayloadLength;
                return true;
            }

            var bytesToCopy = Math.Min(count, _receiveBytesLeft);
            Buffer.BlockCopy(buffer, offset, _receiveBuffer, _receiveOffset, bytesToCopy);
            _receiveOffset += bytesToCopy;
            _receiveBytesLeft -= bytesToCopy;
            offset += bytesToCopy;
            count -= bytesToCopy;
            return _receiveBytesLeft == 0;
        }

        protected override bool WritePayload(byte[] buffer, ref int offset, int count)
        {
            var bytes = Encoding.UTF8.GetBytes(_payloadType.AssemblyQualifiedName, 0,
                _payloadType.AssemblyQualifiedName.Length,
                buffer, offset);
            offset += bytes;
            return true;
        }
    }
}