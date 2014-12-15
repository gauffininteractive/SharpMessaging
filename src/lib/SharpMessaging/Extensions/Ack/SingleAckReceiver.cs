using System;
using System.Threading;
using SharpMessaging.Connection;
using SharpMessaging.Frames;

namespace SharpMessaging.Extensions.Ack
{
    public class SingleAckReceiver : IAckReceiver, IDisposable
    {
        private readonly Timer _checkTimer;
        private readonly IConnection _connection;
        private readonly Action<MessageFrame> _deliverMessageMethod;
        private readonly object _syncLock = new object();
        private MessageFrame _pendingFrame;
        private DateTime _sentAt;

        public SingleAckReceiver(IConnection connection, Action<MessageFrame> deliverMessageMethod)
        {
            _connection = connection;
            _deliverMessageMethod = deliverMessageMethod;
            _checkTimer = new Timer(OnCheckTimer, null, 500, 500);
            AckTimeout = TimeSpan.FromSeconds(10);
        }

        public TimeSpan AckTimeout { get; set; }

        public void AddFrame(MessageFrame frame)
        {
            if (_pendingFrame != null)
                throw new AckException("Already got a pending message.");

            _pendingFrame = frame;
            _sentAt = DateTime.UtcNow;
            _deliverMessageMethod(frame);
        }

        public bool CanSend(MessageFrame frame)
        {
            return _pendingFrame != null;
        }

        public void Confirm(AckFrame ackFrame)
        {
            lock (_syncLock)
            {
               //TODO: Add a sanity check for the sequence number (supporting wrapping numbers)
                _pendingFrame = null;
            }
        }

        public void Reset()
        {
            lock (_syncLock)
            {
                _sentAt = DateTime.MinValue;
                _pendingFrame = null;
            }
        }

        public void Dispose()
        {
            _checkTimer.Dispose();
        }

        public void ConfigureUsingHandshake(HandshakeExtension frame)
        {
        }

        private void OnCheckTimer(object state)
        {
            lock (_syncLock)
            {
                if (_pendingFrame == null)
                    return;

                if (DateTime.UtcNow.Subtract(_sentAt) < AckTimeout)
                    return;

                _deliverMessageMethod(_pendingFrame);
                _sentAt = DateTime.UtcNow;
            }
        }
    }
}