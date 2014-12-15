using System;
using SharpMessaging.Frames;
using SharpMessaging.Payload;

namespace SharpMessaging.Extensions.Payload.DotNet
{
    public class DotNetTypeExtension : IFrameExtension
    {
        public string Name
        {
            get { return "dotnet"; }
        }

        public IFrame CreateFrame(byte extensionId, object payload)
        {
            return new DotNetExtensionFrame(extensionId, payload);
        }

        public IFrame CreateFrame(byte extensionId)
        {
            return new DotNetExtensionFrame(extensionId);
        }

        public HandshakeExtension CreateHandshakeInfo()
        {
            return new HandshakeExtension("dotnet");
        }

        public HandshakeExtension Negotiate(HandshakeExtension remoteEndPointExtension)
        {
            return new HandshakeExtension(Name);
        }

        public void Parse(HandshakeExtension info)
        {
        }

        public IPayloadSerializer CreatePayloadSerializer()
        {
            throw new NotSupportedException();
        }
    }
}