using System;
using System.Threading;
using SharpMessaging.Connection;
using SharpMessaging.Frames;

namespace SharpMessaging.Extensions.Ack
{
    public class SingleAckSender : IAckSender, IDisposable
    {
        private readonly Timer _checkTimer;
        private readonly IConnection _connection;
        private readonly object _syncLock = new object();
        private MessageFrame _pendingFrame;
        private DateTime _sentAt;

        public SingleAckSender(IConnection connection)
        {
            _connection = connection;
            _checkTimer = new Timer(OnCheckTimer, null, 500, 500);
        }

        public TimeSpan AckTimeout { get; set; }

        /// <summary>
        ///     The client can receive more MESSAGE frames from the server.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         i.e. the server is not waiting on the client to ACK previously sent messages.
        ///     </para>
        /// </remarks>
        public bool CanReceiveNewMessageFrames
        {
            get { return _pendingFrame == null; }
        }

        /// <summary>
        ///     Outbound frame.
        /// </summary>
        /// <param name="frame"></param>
        public bool AddFrame(MessageFrame frame)
        {
            if (_pendingFrame != null)
                throw new AckException(string.Format("Already waiting on a pending message (sequence number {0}).",
                    frame.SequenceNumber));

            _pendingFrame = frame;
            _sentAt = DateTime.UtcNow;
            return true;
        }

        public void Reset()
        {
            lock (_syncLock)
            {
                _pendingFrame = null;
                _sentAt = DateTime.MinValue;
            }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _checkTimer.Dispose();
        }

        private void OnCheckTimer(object state)
        {
            lock (_syncLock)
            {
                if (_pendingFrame == null)
                    return;

                if (DateTime.UtcNow.Subtract(_sentAt) < AckTimeout)
                    return;

                _connection.Send(_pendingFrame);
                _sentAt = DateTime.UtcNow;
            }
        }
    }
}