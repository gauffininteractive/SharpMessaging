using System;
using SharpMessaging.Frames;

namespace SharpMessaging.Extensions.Ack
{
    /// <summary>
    ///     Make sure that we receive ACKs for messages that we send.
    /// </summary>
    public interface IAckReceiver : IDisposable
    {
        void AddFrame(MessageFrame frame);
        bool CanSend(MessageFrame frame);

        /// <summary>
        ///     Received an ack frame, use it to ack messages
        /// </summary>
        /// <param name="ackFrame"></param>
        void Confirm(AckFrame ackFrame);
    }
}