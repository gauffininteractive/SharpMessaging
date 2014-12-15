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
    public class BatchAckSender : IAckSender, IDisposable
    {
        private readonly Timer _ackTimer;
        private readonly IConnection _connection;
        private readonly byte _extensionId;
        private readonly object _syncLock = new object();
        private int _amountNotAcked = 0;
        private DateTime _lastAckTime;
        private int _lastAckedSequenceNumber;
        private int _lastReceivedSequenceNumber;
        private FileStream _logStream;
        private int _numberOfFramesToAck;

        public BatchAckSender(IConnection connection, byte extensionId)
        {
            _connection = connection;
            _extensionId = extensionId;
            _ackTimer = new Timer(OnCheckTime, 0, 25, 25);
            Threshold = 10;
            TimeoutBeforeSendingAck = TimeSpan.FromSeconds(1);
        }

        public int Threshold { get; set; }
        public TimeSpan TimeoutBeforeSendingAck { get; set; }

        public bool AddFrame(MessageFrame frame)
        {
            if (_lastAckTime == DateTime.MinValue)
                _lastAckTime = DateTime.UtcNow;

            //send acks directly for frames that we've already acked.
            if (ShouldReAck(frame))
            {
                _lastAckTime = DateTime.UtcNow;
                _lastAckedSequenceNumber = _lastReceivedSequenceNumber;
                _numberOfFramesToAck = 0;
                _connection.Send(new AckFrame(_extensionId, (ushort) _lastReceivedSequenceNumber));
                return false;
            }

            lock (_syncLock)
            {
                _lastReceivedSequenceNumber = frame.SequenceNumber;
                ++_amountNotAcked;
                if (_amountNotAcked < Threshold)
                {
                    return true;
                }

                _lastAckTime = DateTime.UtcNow;
                _lastAckedSequenceNumber = frame.SequenceNumber;
                _connection.Send(new AckFrame(_extensionId, frame.SequenceNumber));
                _amountNotAcked = 0;
            }

            return true;
        }

        
        /// <summary>
        ///     The client can receive more MESSAGE frames from the server.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         i.e. the server is not waiting on the client to ACK previously sent messages.
        ///     </para>
        /// </remarks>
        public bool CanReceiveNewMessageFrames { get; private set; }

        public void Dispose()
        {
            _ackTimer.Dispose();
            if (_logStream != null)
                _logStream.Close();


        }

        public bool ShouldReAck(MessageFrame frame)
        {
            if (frame.SequenceNumber >= _lastAckedSequenceNumber)
                return false;

            // regular sequence and within the last sequence.
            if (_lastAckedSequenceNumber - frame.SequenceNumber < Threshold)
                return true;

            // wrapped sequence (i.e. wrapped to zero after ushort.MaxValue) 
            var span = (ushort.MaxValue - _lastAckedSequenceNumber) + frame.SequenceNumber;
            if (span > Threshold)
                return true;

            return false;
        }

        private void LogMessage(string msg)
        {
            return;
            var logname = @"C:\temp\ackSender.log";
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

        private void OnCheckTime(object state)
        {
            lock (_syncLock)
            {
                if (TimeoutBeforeSendingAck == TimeSpan.Zero)
                    return;

                if (_lastReceivedSequenceNumber == _lastAckedSequenceNumber)
                    return;

                if (DateTime.UtcNow.Subtract(_lastAckTime) >= TimeoutBeforeSendingAck && _amountNotAcked > 0)
                {
                    _amountNotAcked = 0;
                    _lastAckTime = DateTime.UtcNow;
                    _lastAckedSequenceNumber = _lastReceivedSequenceNumber;
                    _numberOfFramesToAck = 0;
                    _connection.Send(new AckFrame(_extensionId, (ushort) _lastReceivedSequenceNumber));
                }
            }
        }

        public static bool IsAckedMessage(int msgSequenceNumber, int sequenceNumberToCompareWith, int rangeCount)
        {
            // wrapped series or previously acked number
            if (msgSequenceNumber < sequenceNumberToCompareWith)
            {
                var number = ushort.MaxValue - sequenceNumberToCompareWith + msgSequenceNumber;
                return number < rangeCount;
            }

            //new msg or wrapped series
            return false;
        }
    }
}