using SharpMessaging.Payload;

namespace SharpMessaging.Frames
{
    public interface IPayloadExtension
    {
        IPayloadSerializer CreatePayloadSerializer();
    }


    public interface IFrameExtension
    {
        string Name { get; }
        IFrame CreateFrame(byte extensionId, object payload);
        IFrame CreateFrame(byte extensionId);
        HandshakeExtension CreateHandshakeInfo();
        HandshakeExtension Negotiate(HandshakeExtension remoteEndPointExtension);
    }

    public interface IExtensionProcessor
    {
        void Process();
    }
}