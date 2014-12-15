using System;
using SharpMessaging.Frames;
using SharpMessaging.Payload;

namespace SharpMessaging.fastJSON
{
    public class FastJsonExtension : IFrameExtension, IPayloadExtension
    {
        public string Name
        {
            get { return "json"; }
        }

        public IFrame CreateFrame(byte extensionId, object payload)
        {
            throw new NotSupportedException();
        }

        public IFrame CreateFrame(byte extensionId)
        {
            throw new NotSupportedException();
        }

        public HandshakeExtension CreateHandshakeInfo()
        {
            return new HandshakeExtension(Name);
        }

        public HandshakeExtension Negotiate(HandshakeExtension remoteEndPointExtension)
        {
            return new HandshakeExtension(Name);
        }

        public IPayloadSerializer CreatePayloadSerializer()
        {
            return new FastJsonSerializer();
        }

        public void Parse(HandshakeExtension info)
        {
        }
    }
}