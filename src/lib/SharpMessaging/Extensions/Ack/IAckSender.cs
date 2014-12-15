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
        ///     Frame that we should ack
        /// </summary>
        /// <param name="frame"><c>true</c> if we have not previously received this frame; otherwise <c>false</c>.</param>
        bool AddFrame(MessageFrame frame);
    }
}