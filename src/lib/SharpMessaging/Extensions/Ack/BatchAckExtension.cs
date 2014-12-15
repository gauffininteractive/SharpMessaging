using System;
using System.Collections.Generic;
using SharpMessaging.Connection;
using SharpMessaging.Frames;

namespace SharpMessaging.Extensions.Ack
{
    public class BatchAckExtension : IAckExtension, IFrameExtension
    {
        public BatchAckExtension()
        {
            MessagesPerAck = 10;
            AckExpireTime = TimeSpan.FromMilliseconds(500);
        }

        /// <summary>
        ///     Number of messages that can be sent until an ack is required.
        /// </summary>
        public int MessagesPerAck { get; set; }

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
        public TimeSpan AckExpireTime { get; set; }

        /// <summary>
        ///     Amount of messages that can be pending for a write (i.e. may not be sent yet due to previous messages have not been
        ///     sent)
        /// </summary>
        public int MaxAmountOfPendingMessages { get; set; }


        public IAckReceiver CreateAckReceiver(IConnection connection, byte extensionId,
            Action<MessageFrame> deliverMessageMethod, HandshakeExtension extProperties)
        {
            var msgsPerAck = MessagesPerAck;
            if (extProperties.Properties.ContainsKey("MessagesPerAck"))
                msgsPerAck = int.Parse(extProperties.Properties["MessagesPerAck"]);

            var expire = AckExpireTime;
            if (extProperties.Properties.ContainsKey("AckExpireTime"))
                expire = TimeSpan.FromMilliseconds(int.Parse(extProperties.Properties["AckExpireTime"]));

            return new BatchAckReceiver(connection, deliverMessageMethod, MaxAmountOfPendingMessages)
            {
                Threshold = msgsPerAck,
                TimeoutBeforeResendingMessage = expire.Add(TimeSpan.FromMilliseconds(20)) //for network latency
            };
        }

        public IAckSender CreateAckSender(IConnection connection, byte extensionId, HandshakeExtension extProperties)
        {
            var msgsPerAck = MessagesPerAck;
            if (extProperties.Properties.ContainsKey("MessagesPerAck"))
                msgsPerAck = int.Parse(extProperties.Properties["MessagesPerAck"]);

            var expire = AckExpireTime;
            if (extProperties.Properties.ContainsKey("AckExpireTime"))
                expire = TimeSpan.FromMilliseconds(int.Parse(extProperties.Properties["AckExpireTime"]));

            return new BatchAckSender(connection, extensionId)
            {
                Threshold = msgsPerAck,
                TimeoutBeforeSendingAck = expire
            };
        }


        public string Name
        {
            get { return "batch-ack"; }
        }

        public IFrame CreateFrame(byte extensionId, object sequenceNumber)
        {
            return new AckFrame(extensionId, (ushort) sequenceNumber);
        }

        public IFrame CreateFrame(byte extensionId)
        {
            return new AckFrame(extensionId, 0);
        }

        public HandshakeExtension CreateHandshakeInfo()
        {
            var properties = new Dictionary<string, string>();
            if (MessagesPerAck > 0)
                properties.Add("MessagesPerAck", MessagesPerAck.ToString());
            if (AckExpireTime != TimeSpan.Zero)
                properties.Add("AckExpireTime", AckExpireTime.TotalMilliseconds.ToString());

            return new HandshakeExtension(Name, properties);
        }


        public HandshakeExtension Negotiate(HandshakeExtension remoteEndPointExtension)
        {
            var frame = new HandshakeExtension(Name);

            if (remoteEndPointExtension.Properties.ContainsKey("MessagesPerAck"))
            {
                var value = Math.Max(int.Parse(remoteEndPointExtension.Properties["MessagesPerAck"]), MessagesPerAck);
                frame.Properties.Add("MessagesPerAck", value.ToString());
            }

            if (remoteEndPointExtension.Properties.ContainsKey("AckExpireTime"))
            {
                var value = Math.Min(int.Parse(remoteEndPointExtension.Properties["AckExpireTime"]),
                    AckExpireTime.TotalMilliseconds);
                frame.Properties.Add("AckExpireTime", value.ToString());
            }

            return frame;
        }
    }
}