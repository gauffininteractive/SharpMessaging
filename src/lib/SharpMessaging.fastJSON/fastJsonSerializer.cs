using System;
using System.IO;
using System.Text;
using fastJSON;
using SharpMessaging.Frames;
using SharpMessaging.Payload;

namespace SharpMessaging.fastJSON
{
    public class fastJsonSerializer : IPayloadSerializer
    {
        private Encoding _encoding = Encoding.UTF8;

        public Encoding Encoding
        {
            get { return _encoding; }
            set { _encoding = value; }
        }

        public object Deserialize(Type type, byte[] buffer, int offset, int count)
        {
            var str = _encoding.GetString(buffer, offset, count);
            return JSON.ToObject(str, type);
        }

        public object Deserialize(byte[] buffer, int offset, int count)
        {
            var str = _encoding.GetString(buffer, offset, count);
            return JSON.ToObject(str);
        }

        public object Deserialize(Type type, Stream source)
        {
            var reader = new StreamReader(source);
            var str = reader.ReadToEnd();
            return JSON.ToObject(str, type);
        }

        public object Deserialize(Stream source)
        {
            var reader = new StreamReader(source);
            var str = reader.ReadToEnd();
            return JSON.ToObject(str);
        }

        public void Serialize(MessageFrame frame)
        {
            var str = JSON.ToJSON(frame.Payload);
            if (frame.PayloadBuffer.Count >= Encoding.UTF8.GetByteCount(str))
            {
                var buf = frame.PayloadBuffer;
                var count = Encoding.UTF8.GetBytes(str, 0, str.Length, buf.Array, buf.Offset);
                frame.PayloadBuffer = new ArraySegment<byte>(buf.Array, buf.Offset, count);
            }
            else
            {
                var buf = Encoding.UTF8.GetBytes(str);
                frame.PayloadBuffer = new ArraySegment<byte>(buf, 0, buf.Length);
            }
        }
    }
}