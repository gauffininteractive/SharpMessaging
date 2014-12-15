using System;
using SharpMessaging.Connection;
using SharpMessaging.Frames;

namespace SharpMessaging.Extensions.Ack
{
    public class SingleAckExtension : IAckExtension, IFrameExtension
    {
        public TimeSpan AckExpireTime { get; set; }

        public IAckReceiver CreateAckReceiver(IConnection connection, byte extensionId,
            Action<MessageFrame> deliverMessageMethod, HandshakeExtension extProperties)
        {
            var receiver = new SingleAckReceiver(connection, deliverMessageMethod);
            if (extProperties.Properties.ContainsKey("AckExpireTime"))
            {
                var value = Math.Min(int.Parse(extProperties.Properties["AckExpireTime"]),
                    AckExpireTime.TotalMilliseconds);
                receiver.AckTimeout = TimeSpan.FromMilliseconds(value);
            }
            return receiver;
        }

        public IAckSender CreateAckSender(IConnection connection, byte extensionId, HandshakeExtension extProperties)
        {
            return new SingleAckSender(connection);
        }

        public string Name
        {
            get { return "ack"; }
        }

        /// <summary>
        /// </summary>
        /// <param name="extensionId"></param>
        /// <param name="payload">Should be the sequence number to ack</param>
        /// <returns></returns>
        public IFrame CreateFrame(byte extensionId, object payload)
        {
            return new AckFrame(extensionId, (ushort) payload);
        }

        public IFrame CreateFrame(byte extensionId)
        {
            return new AckFrame(extensionId, 0);
        }

        public HandshakeExtension CreateHandshakeInfo()
        {
            var frame = new HandshakeExtension(Name);
            frame.Properties.Add("AckExpireTime", AckExpireTime.TotalMilliseconds.ToString());
            return frame;
        }

        public HandshakeExtension Negotiate(HandshakeExtension remoteEndPointExtension)
        {
            var frame = new HandshakeExtension(Name);

            if (remoteEndPointExtension.Properties.ContainsKey("AckExpireTime"))
            {
                var value = Math.Min(int.Parse(remoteEndPointExtension.Properties["AckExpireTime"]),
                    AckExpireTime.TotalMilliseconds);
                frame.Properties.Add("AckExpireTime", value.ToString());
            }

            return frame;
        }

        public void Parse(HandshakeExtension info)
        {
            if (info.Properties.ContainsKey("AckExpireTime"))
                AckExpireTime = TimeSpan.FromMilliseconds(int.Parse(info.Properties["AckExpireTime"]));
        }
    }
}