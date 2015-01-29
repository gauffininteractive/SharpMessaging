using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using SharpMessaging.Connection;
using SharpMessaging.Frames;

namespace SharpMessaging.Extensions.Ack
{
    /// <summary>
    ///     Resends messages that have not been acked.
    /// </summary>
    public class AckReceiver : IAckReceiver, IDisposable
    {
        private readonly IConnection _connection;
        private readonly Action<MessageFrame> _deliverMessageMethod;
        private readonly int _messagesPerAck;
        private readonly CircularQueueList<FrameWrapper> _framesToAck;
        private readonly Timer _timer;
        private bool _isFirstAck;
        private int _lastSeq;
        private FileStream _logStream;

        public AckReceiver(IConnection connection, Action<MessageFrame> deliverMessageMethod,
            int messagesPerAck)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (deliverMessageMethod == null) throw new ArgumentNullException("deliverMessageMethod");
            if (messagesPerAck <= 0)
                throw new ArgumentOutOfRangeException("messagesPerAck", messagesPerAck, "Must be a positive number");

            _framesToAck = new CircularQueueList<FrameWrapper>(messagesPerAck);
            _connection = connection;
            _deliverMessageMethod = deliverMessageMethod;
            _messagesPerAck = messagesPerAck;
            _timer = new Timer(ResendMessages, null, 50, 50);

            TimeoutBeforeResendingMessage = TimeSpan.FromSeconds(10);
        }

        public TimeSpan TimeoutBeforeResendingMessage { get; set; }


        public void Send(MessageFrame frame)
        {
            if (_framesToAck.Count >= _messagesPerAck)
                throw new AckException(string.Format("There are already {0} awaiting an ACK. HOLD ON!",
                    _framesToAck.Count));

            _lastSeq = frame.SequenceNumber;
            _framesToAck.Enqueue(new FrameWrapper(frame));
            _deliverMessageMethod(frame);
        }

        public int FreeSlots { get { return _messagesPerAck - _framesToAck.Count; }}

        public bool CanSend(MessageFrame frame)
        {
            return _framesToAck.Count < _messagesPerAck;
        }

        public int Confirm(AckFrame ackFrame)
        {
            int frameCount = 0;
            var sequenceNumber = ackFrame.SequenceNumber;
            lock (_framesToAck)
            {
                while (_framesToAck.Count > 0)
                {
                    var item = _framesToAck.Dequeue();
                    ++frameCount;
                    if (item.Frame.SequenceNumber == sequenceNumber)
                        break;
                }

                var msgsToAck = Math.Min(_messagesPerAck, _framesToAck.Count);
                if (msgsToAck == 0)
                    return frameCount;

                for (var i = 0; i < msgsToAck; i++)
                {
                    _framesToAck[i].MarkAsSent();
                    _deliverMessageMethod(_framesToAck[i].Frame);
                }
            }

            return frameCount;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        private void ResendMessages(object state)
        {
            try
            {
                if (TimeoutBeforeResendingMessage == TimeSpan.Zero)
                    return;

                lock (_framesToAck)
                {
                    if (_framesToAck.Count == 0)
                        return;

                    if (!_framesToAck.Peek().HaveExpired(TimeoutBeforeResendingMessage))
                        return;

                    for (var i = 0; i < _framesToAck.Count; i++)
                    {
                        if (!_framesToAck[i].HaveExpired(TimeoutBeforeResendingMessage))
                            break;
                        _connection.Send(_framesToAck[i].Frame);
                        _framesToAck[i].SentAgain();
                    }
                }
            }
            catch (Exception exception)
            {
                //TODO: Die?
            }
        }

        private class FrameWrapper
        {
            private readonly DateTime _addedAt;
            private DateTime _sentTime;

            public FrameWrapper(MessageFrame frame)
            {
                Frame = frame;
                _addedAt = DateTime.UtcNow;
            }

            public MessageFrame Frame { get; private set; }

            public DateTime ArrivalTime
            {
                get { return _addedAt; }
            }

            public bool HaveExpired(TimeSpan ackTimeout)
            {
                if (_sentTime == DateTime.MinValue)
                    return false;

                return DateTime.UtcNow.Subtract(_sentTime) >= ackTimeout;
            }

            public void SentAgain()
            {
                _sentTime = DateTime.UtcNow;
            }

            public void MarkAsSent()
            {
                _sentTime = DateTime.UtcNow;
            }
        }
    }
}