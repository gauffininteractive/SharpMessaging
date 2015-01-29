using System;
using SharpMessaging.Frames;

namespace SharpMessaging.Extensions.Ack
{
    /// <summary>
    ///     Make sure that we send ACKs for messages that we receive.
    /// </summary>
    public interface IAckSender : IDisposable
    {
        /// <summary>
        ///     The client can receive more MESSAGE frames from the server.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         i.e. the server is not waiting on the client to ACK previously sent messages.
        ///     </para>
        /// </remarks>
        bool CanReceiveNewMessageFrames { get; }

        /// <summary>
        /// check if we've received this frame already (and should therefore re-ack it instead of processing it)
        /// </summary>
        /// <param name="frame">frame to check (received from remote end point)</param>
        /// <returns><c>true</c> if we should resend the ack for it; <c>false</c> means that we can process it.</returns>
        bool ShouldReAck(MessageFrame frame);

        /// <summary>
        ///     Frame that we should ack
        /// </summary>
        /// <param name="frame"><c>true</c> if we have not previously received this frame; otherwise <c>false</c>.</param>
        bool AckFrame(MessageFrame frame);
    }
}