using System;
using SharpMessaging.Connection;
using SharpMessaging.Frames;

namespace SharpMessaging.Extensions.Ack
{
    public interface IAckExtension
    {
        /// <summary>
        ///     Number of messages that can be sent until an ack is required.
        /// </summary>
        int MessagesPerAck { get; set; }

        /// <summary>
        ///     Amount if time before the ack is considered to have expired.
        /// </summary>
        /// <returns>
        ///     <para>
        ///         Means that the party that delivered the message must resend it when the amount of time between the message
        ///         sending and now have passed the specified time span..
        ///     </para>
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The value is transported as milliseconds.
        ///     </para>
        /// </remarks>
        TimeSpan AckExpireTime { get; set; }

        IAckReceiver CreateAckReceiver(IConnection connection, byte extensionId,
            Action<MessageFrame> deliverMessageMethod, HandshakeExtension extProperties);

        IAckSender CreateAckSender(IConnection connection, byte extensionId, HandshakeExtension extProperties);
    }
}