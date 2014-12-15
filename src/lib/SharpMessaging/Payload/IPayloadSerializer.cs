using System;
using System.IO;
using SharpMessaging.Frames;

namespace SharpMessaging.Payload
{
    public interface IPayloadSerializer
    {
        object Deserialize(Type type, byte[] buffer, int offset, int count);
        object Deserialize(byte[] buffer, int offset, int count);
        object Deserialize(Type type, Stream source);
        object Deserialize(Stream source);
        void Serialize(MessageFrame frame);
    }
}