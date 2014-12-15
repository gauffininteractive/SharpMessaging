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
    public class BatchAckReceiver : IAckReceiver, IDisposable
    {
        private static readonly List<ushort> _receivedMessages = new List<ushort>(1000000);
        private readonly IConnection _connection;
        private readonly Action<MessageFrame> _deliverMessageMethod;
        private readonly CircularQueueList<FrameWrapper> _framesToAck;
        private readonly int _maxAmountOfPendingMessages;
        private readonly Timer _timer;
        private bool _isFirstAck;
        private int _lastSeq;
        private FileStream _logStream;
        private int _sendCounter = 0;

        public BatchAckReceiver(IConnection connection, Action<MessageFrame> deliverMessageMethod,
            int maxAmountOfPendingMessages)
        {
            if (maxAmountOfPendingMessages == 0)
            {
                maxAmountOfPendingMessages = 1000;
                _framesToAck = new CircularQueueList<FrameWrapper>(maxAmountOfPendingMessages);
            }
            else
                _framesToAck = new CircularQueueList<FrameWrapper>(maxAmountOfPendingMessages);

            if (maxAmountOfPendingMessages > 100)
                Threshold = maxAmountOfPendingMessages/10;
            else
                Threshold = 10;

            _connection = connection;
            _deliverMessageMethod = deliverMessageMethod;
            _maxAmountOfPendingMessages = maxAmountOfPendingMessages;
            _timer = new Timer(ResendMessages, null, 50, 50);

            TimeoutBeforeResendingMessage = TimeSpan.FromSeconds(10);
        }

        public TimeSpan TimeoutBeforeResendingMessage { get; set; }
        public int Threshold { get; set; }


        public void AddFrame(MessageFrame frame)
        {
            if (_framesToAck.Count >= _maxAmountOfPendingMessages)
                throw new AckException(string.Format("There are already {0} awaiting an ACK. HOLD ON!",
                    _framesToAck.Count));

            _receivedMessages.Add(frame.SequenceNumber);
            if (frame.SequenceNumber < _lastSeq)
                LogMessage("ERROR: " + frame.SequenceNumber + ", last: " + _lastSeq);
            _lastSeq = frame.SequenceNumber;


            lock (_framesToAck)
            {
                //less since we should include this message in the count
                if (_framesToAck.Count < Threshold)
                {
                    _sendCounter++;
                    _framesToAck.Enqueue(new FrameWrapper(frame));
                    _deliverMessageMethod(frame);
                }
                else
                {
                    _framesToAck.Enqueue(new FrameWrapper(frame));
                }
            }
        }

        public bool CanSend(MessageFrame frame)
        {
            return _framesToAck.Count < Threshold;
        }

        public void Confirm(AckFrame ackFrame)
        {
            var sequenceNumber = ackFrame.SequenceNumber;
            lock (_framesToAck)
            {
                while (_framesToAck.Count > 0)
                {
                    var item = _framesToAck.Dequeue();
                    if (item.Frame.SequenceNumber == sequenceNumber)
                        break;
                }

                var msgsToAck = Math.Min(Threshold, _framesToAck.Count);
                if (msgsToAck == 0)
                    return;

                for (var i = 0; i < msgsToAck; i++)
                {
                    _sendCounter++;
                    _framesToAck[i].MarkAsSent();
                    _deliverMessageMethod(_framesToAck[i].Frame);
                }
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        private void LogMessage(string msg)
        {
            return;
            var logname = @"C:\temp\ackReceiver.log";
            if (_logStream == null)
            {
                if (File.Exists(logname))
                    File.Delete(logname);

                _logStream = new FileStream(logname, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 8192,
                    FileOptions.SequentialScan);
            }

            var buf =
                Encoding.ASCII.GetBytes(DateTime.UtcNow.ToString("HH:mm:ss.fff") + " " + msg + "\r\n");
            _logStream.Write(buf, 0, buf.Length);
            _logStream.Flush();
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