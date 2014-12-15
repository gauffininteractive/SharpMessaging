using System;
using SharpMessaging.Connection;
using SharpMessaging.Frames;

namespace SharpMessaging.Extensions.Ack
{
    public interface IAckExtension
    {
        IAckReceiver CreateAckReceiver(IConnection connection, byte extensionId,
            Action<MessageFrame> deliverMessageMethod, HandshakeExtension extProperties);

        IAckSender CreateAckSender(IConnection connection, byte extensionId, HandshakeExtension extProperties);
    }
}