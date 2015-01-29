using System;
using SharpMessaging.Frames;

namespace SharpMessaging.Extensions.Ack
{
    /// <summary>
    ///     Make sure that we receive ACKs for messages that we send.
    /// </summary>
    public interface IAckReceiver : IDisposable
    {
        /// <summary>
        /// Amount of messages that can be appended.
        /// </summary>
        int FreeSlots { get; }

        /// <summary>
        ///     We can send another message directly.
        /// </summary>
        /// <param name="frame">Frame that we want to send</param>
        /// <returns><c>true</c> if the pending frame threshold have not been reached yet; otherwise <c>false</c>.</returns>
        bool CanSend(MessageFrame frame);

        /// <summary>
        ///     Received an ack frame, use it to ack messages
        /// </summary>
        /// <param name="ackFrame"></param>
        /// <returns>Number of frames that was confirmed (as acks can be accumulative).</returns>
        int Confirm(AckFrame ackFrame);

        /// <summary>
        ///     Send frame (and wait for ack)
        /// </summary>
        /// <param name="frame">frame to send</param>
        /// <exception cref="InvalidOperationException">
        ///     If pending frame threshold have been reached (i.e. we may not send any more
        ///     frames before an ack have been received).
        /// </exception>
        void Send(MessageFrame frame);
    }
}