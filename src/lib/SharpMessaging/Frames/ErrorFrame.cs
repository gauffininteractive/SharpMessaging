using System;
using System.Text;
using SharpMessaging.Connection;

namespace SharpMessaging.Frames
{
    internal class ErrorFrame : IFrame
    {
        private readonly string _errorMessage;

        public ErrorFrame(string errorMessage)
        {
            _errorMessage = errorMessage;
        }

        public void ResetWrite(WriterContext context)
        {
        }

        public bool Write(WriterContext context)
        {
            var buf = new byte[65535];
            var contentLength = Encoding.UTF8.GetByteCount(_errorMessage);
            var offset = 2;
            if (contentLength <= 512)
            {
                buf[0] = (byte) (FrameFlags.ErrorFrame | FrameFlags.LargeFrame);
                buf[1] = (byte) contentLength;
            }
            else
            {
                buf[0] = (byte) FrameFlags.ErrorFrame;
                var lenBuf = BitConverter.GetBytes(contentLength);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lenBuf);
                buf[1] = lenBuf[0];
                buf[2] = lenBuf[1];
                buf[3] = lenBuf[2];
                buf[4] = lenBuf[3];
                offset = 5;
            }

            Encoding.UTF8.GetBytes(_errorMessage, 0, _errorMessage.Length, buf, offset);
            context.Enqueue(buf, 0, offset + contentLength);
            return true;
        }

        public void ResetRead()
        {
        }

        public bool Read(byte[] buffer, ref int offset, ref int bytesToProcess)
        {
            throw new NotImplementedException();
        }

        public void WriteCompleted(WriterContext context)
        {
        }
    }
}