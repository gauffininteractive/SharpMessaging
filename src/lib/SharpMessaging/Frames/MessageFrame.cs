using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using SharpMessaging.Connection;

namespace SharpMessaging.Frames
{
    public sealed class MessageFrame : IFrame
    {
        public static int BufferLimit = 16384;
        private readonly byte[] _stateBuffer = new byte[256];
        public string Destination;
        public object Payload;
        private byte[] _internalPayloadBuffer;
        private ArraySegment<byte> _payloadBuffer = WriterContext.EmptySegment;
        private Stream _payloadStream;
        private Dictionary<string, string> _properties;
        private int _readOffset;
        private int _receiveBytesLeft;
        private int _receivePayloadLength;
        private MessageFrameState _state = MessageFrameState.Flags;
        private int _stateLength;
        private ArraySegment<byte> _writeBuffer;
        private int _writeBytesLeftCorCurrentState;
        private int _writePayloadBufferOffset;

        public MessageFrame()
        {
            Destination = "";
            _receiveBytesLeft = -1;
            _writeBytesLeftCorCurrentState = -1;
            //_payloadBuffer = new ArraySegment<byte>(_internalPayloadBuffer);
        }

        public MessageFrame(object payload)
        {
            if (payload is ArraySegment<byte>)
                PayloadBuffer = (ArraySegment<byte>) payload;
            else if (payload is byte[])
                PayloadBuffer = new ArraySegment<byte>((byte[]) payload);
            else if (payload is string)
            {
                PayloadBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes((string)payload));
            }
            else
                Payload = payload;

            Destination = "";
            _receiveBytesLeft = -1;
            _writeBytesLeftCorCurrentState = -1;
            //_payloadBuffer = new ArraySegment<byte>(_internalPayloadBuffer);
        }

        public FrameFlags Flags { get; set; }
        public ushort SequenceNumber { get; set; }

        public ArraySegment<byte> PayloadBuffer
        {
            get { return _payloadBuffer; }
            set
            {
                _payloadBuffer = value;
                if (value.Count == 0 && !ReferenceEquals(value.Array, WriterContext.EmptySegment.Array))
                    Debugger.Break();
            }
        }

        public Stream PayloadStream
        {
            get { return _payloadStream; }
            set { _payloadStream = value; }
        }

        public bool IsFlaggedAsSmall
        {
            get { return (Flags & FrameFlags.LargeFrame) == 0; }
        }

        public IDictionary<string, string> Properties
        {
            get
            {
                if (_properties == null)
                    _properties = new Dictionary<string, string>();
                return _properties;
            }
        }


        /// <summary>
        ///     Connection failed. Reset state until  a new connection is received.
        /// </summary>
        public void ResetRead()
        {
            _receiveBytesLeft = -1;
            _stateLength = -1;
            _state = MessageFrameState.Flags;
            _payloadBuffer = WriterContext.EmptySegment;
            _readOffset = 0;
            Properties.Clear();
            if (_payloadStream != null)
            {
                _payloadStream.Close();
                _payloadStream = null;
            }
        }

        public bool Read(byte[] buffer, ref int offset, ref int bytesTransferred)
        {
            var numberOfBytesTransferredFromStart = bytesTransferred;
            var allCompleted = false;
            while (bytesTransferred > 0 && !allCompleted)
            {
                bool isBufferCopyCompleted;
                switch (_state)
                {
                    case MessageFrameState.Flags:
                        if (buffer[offset] == 32)
                        {
                            throw new BackTrackException("", offset);
                        }


                        Flags = (FrameFlags) buffer[offset];
                        _state = MessageFrameState.SequenceNumber;
                        ++offset;
                        --bytesTransferred;
                        _receiveBytesLeft = 2;
                        break;
                    case MessageFrameState.SequenceNumber:
                        isBufferCopyCompleted = CopyToReadBuffer(buffer, ref offset, ref bytesTransferred);
                        if (isBufferCopyCompleted)
                        {
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(_stateBuffer, 0, 2);
                            SequenceNumber = BitConverter.ToUInt16(_stateBuffer, 0);
                            _state = MessageFrameState.DestinationLength;
                        }
                        break;
                    case MessageFrameState.DestinationLength:
                        _stateLength = _receiveBytesLeft = buffer[offset];
                        _state = _stateLength == 0 ? MessageFrameState.FilterLength : MessageFrameState.Destination;
                        ++offset;
                        --bytesTransferred;
                        if (_state == MessageFrameState.FilterLength)
                        {
                            _receiveBytesLeft = 2;
                        }
                        break;

                    case MessageFrameState.Destination:
                        isBufferCopyCompleted = CopyToReadBuffer(buffer, ref offset, ref bytesTransferred);
                        if (isBufferCopyCompleted)
                        {
                            Destination = Encoding.ASCII.GetString(_stateBuffer, 0, _stateLength);
                            _receiveBytesLeft = 2;
                            _state = MessageFrameState.FilterLength;
                        }
                        break;

                    case MessageFrameState.FilterLength:
                        isBufferCopyCompleted = CopyToReadBuffer(buffer, ref offset, ref bytesTransferred);
                        if (isBufferCopyCompleted)
                        {
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(_stateBuffer, 0, 2);
                            _stateLength = _receiveBytesLeft = BitConverter.ToInt16(_stateBuffer, 0);
                            _state = _stateLength == 0 ? MessageFrameState.PayloadLength : MessageFrameState.Filter;
                            if (_state == MessageFrameState.PayloadLength)
                            {
                                _receiveBytesLeft = IsFlaggedAsSmall ? 1 : 4;
                            }
                        }
                        break;

                    case MessageFrameState.Filter:
                        isBufferCopyCompleted = CopyToReadBuffer(buffer, ref offset, ref bytesTransferred);
                        if (isBufferCopyCompleted)
                        {
                            var filters = Encoding.ASCII.GetString(_stateBuffer, 0, _stateLength);
                            DecodeFilters(filters);
                            _state = MessageFrameState.PayloadLength;
                            _receiveBytesLeft = IsFlaggedAsSmall ? 1 : 4;
                        }
                        break;

                    case MessageFrameState.PayloadLength:
                        if (IsFlaggedAsSmall)
                        {
                            _receivePayloadLength = buffer[offset];
                            _state = MessageFrameState.SmallPayload;
                            _receiveBytesLeft = _receivePayloadLength;
                            ++offset;
                            --bytesTransferred;
                        }
                        else
                        {
                            isBufferCopyCompleted = CopyToReadBuffer(buffer, ref offset, ref bytesTransferred);
                            if (isBufferCopyCompleted)
                            {
                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(_stateBuffer, 0, 4);
                                _receivePayloadLength = BitConverter.ToInt32(_stateBuffer, 0);
                                _receiveBytesLeft = _receivePayloadLength;
                                _state = _receivePayloadLength <= BufferLimit
                                    ? MessageFrameState.SmallPayload
                                    : MessageFrameState.LargePayload;
                            }
                        }
                        if (_state == MessageFrameState.SmallPayload)
                        {
                            if (_internalPayloadBuffer == null || _payloadBuffer.Array.Length < _receivePayloadLength)
                            {
                                _internalPayloadBuffer = new byte[_receivePayloadLength*2];
                            }
                        }
                        break;
                    case MessageFrameState.SmallPayload:
                        var bytesToCopy = Math.Min(_receiveBytesLeft, bytesTransferred);
                        Buffer.BlockCopy(buffer, offset, _internalPayloadBuffer, _readOffset, bytesToCopy);
                        _receiveBytesLeft -= bytesToCopy;
                        bytesTransferred -= bytesToCopy;
                        offset += bytesToCopy;
                        if (_receiveBytesLeft == 0)
                        {
                            _payloadBuffer = new ArraySegment<byte>(_internalPayloadBuffer, 0, _receivePayloadLength);
                            allCompleted = true;
                        }
                        else
                        {
                            _readOffset += bytesToCopy;
                        }
                        break;

                    case MessageFrameState.LargePayload:
                        allCompleted = true;
                        break;
                }
            }
            if (offset < 0)
                throw new InvalidOperationException();
            return allCompleted;
        }

        /// <summary>
        ///     Connection have been lost. Reset state and return buffers.
        /// </summary>
        /// <param name="context"></param>
        public void ResetWrite(WriterContext context)
        {
            if (_writeBuffer != WriterContext.EmptySegment)
            {
                context.ReturnBuffer(_writeBuffer);
                _writeBuffer = WriterContext.EmptySegment;
            }

            _writePayloadBufferOffset = 0;
            _writeBytesLeftCorCurrentState = -1;
        }

        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="context">Used to enqueue bytes for delivery.</param>
        /// <returns><c>true</c> if more buffers can be appened.</returns>
        public bool Write(WriterContext context)
        {
            if (_writeBytesLeftCorCurrentState != -1)
            {
                var bytesToCopy = Math.Min(_writeBuffer.Count, _writeBytesLeftCorCurrentState);
                bytesToCopy = Math.Min(bytesToCopy, context.BytesLeftToEnqueue);
                if (bytesToCopy < 10)
                    return false;

                if (_payloadStream != null)
                {
                    _payloadStream.Write(_writeBuffer.Array, 0, bytesToCopy);
                }
                else
                {
                    Buffer.BlockCopy(_payloadBuffer.Array, _writePayloadBufferOffset, _writeBuffer.Array,
                        _writeBuffer.Offset, bytesToCopy);
                }
                _writeBytesLeftCorCurrentState -= bytesToCopy;
                _writePayloadBufferOffset += bytesToCopy;
                context.Enqueue(new ArraySegment<byte>(_writeBuffer.Array, _writeBuffer.Offset, bytesToCopy));
                if (_writeBytesLeftCorCurrentState == 0)
                {
                    return true;
                }

                return false;
            }

            _writeBuffer = context.DequeueBuffer();
            _writePayloadBufferOffset = 0;
            var offset = _writeBuffer.Offset;
            var payloadLength = 0;
            if (_payloadStream != null)
            {
                _writeBuffer.Array[offset++] = _payloadStream.Length > byte.MaxValue
                    ? (byte) FrameFlags.LargeFrame
                    : (byte) 0;
                payloadLength = (int) _payloadStream.Length;
            }
            else
            {
                if(_writeBuffer == WriterContext.EmptySegment)
                    throw new InvalidOperationException("If no serializer is used, you have to use either PayloadStream or PayloadBuffer property.");
                    
                _writeBuffer.Array[offset++] = PayloadBuffer.Count > byte.MaxValue
                    ? (byte) FrameFlags.LargeFrame
                    : (byte) 0;
                payloadLength = PayloadBuffer.Count;
            }

            var buf = BitConverter.GetBytes(SequenceNumber);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            _writeBuffer.Array[offset++] = buf[0];
            _writeBuffer.Array[offset++] = buf[1];
            _writeBuffer.Array[offset++] = (byte) Destination.Length;
            if (Destination.Length > 0)
            {
                Encoding.ASCII.GetBytes(Destination, 0, Destination.Length, _writeBuffer.Array, offset);
                offset += Destination.Length;
            }

            if (_properties != null && _properties.Count > 0)
            {
                var filters = EncodeFilters();
                var filterLen = BitConverter.GetBytes((short) filters.Length);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(filterLen);
                _writeBuffer.Array[offset++] = filterLen[0];
                _writeBuffer.Array[offset++] = filterLen[1];
                Encoding.ASCII.GetBytes(filters, 0, filters.Length, _writeBuffer.Array, offset);
                offset += filters.Length;
            }
            else
            {
                _writeBuffer.Array[offset++] = 0;
                _writeBuffer.Array[offset++] = 0;
            }

            //length
            if (payloadLength > byte.MaxValue)
            {
                buf = BitConverter.GetBytes(payloadLength);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(buf);
                _writeBuffer.Array[offset++] = buf[0];
                _writeBuffer.Array[offset++] = buf[1];
                _writeBuffer.Array[offset++] = buf[2];
                _writeBuffer.Array[offset++] = buf[3];
            }
            else
            {
                _writeBuffer.Array[offset++] = (byte) payloadLength;
            }


            _writeBytesLeftCorCurrentState = payloadLength;
            var payloadBytesToCopy = Math.Min(_writeBuffer.Count - (offset - _writeBuffer.Offset),
                _writeBytesLeftCorCurrentState);
            if (_payloadStream != null)
            {
                _payloadStream.Write(_writeBuffer.Array, offset, payloadBytesToCopy);
            }
            else
            {
                Buffer.BlockCopy(_payloadBuffer.Array, _writePayloadBufferOffset, _writeBuffer.Array, offset,
                    payloadBytesToCopy);
            }
            payloadBytesToCopy = Math.Min(payloadBytesToCopy, context.BytesLeftToEnqueue);
            _writeBytesLeftCorCurrentState -= payloadBytesToCopy;
            _writePayloadBufferOffset += payloadBytesToCopy;
            var size = (offset - _writeBuffer.Offset) + payloadBytesToCopy;
            context.Enqueue(_writeBuffer.Array, _writeBuffer.Offset, size);

            if (_writeBytesLeftCorCurrentState == 0)
                return true;
            return false;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CopyToReadBuffer(byte[] buffer, ref int offset, ref int bytesTransferred)
        {
            var bytesToCopy = Math.Min(_receiveBytesLeft, bytesTransferred);
            if (offset + bytesToCopy > buffer.Length)
                throw new ArgumentOutOfRangeException("offset", offset,
                    "Too large: " + offset + "+" + bytesToCopy + "<" + buffer.Length);
            if (_readOffset + bytesToCopy > _stateBuffer.Length)
                throw new ArgumentOutOfRangeException("offset", offset,
                    "Too large for state buffer: " + _readOffset + "+" + bytesToCopy + "<" + _stateBuffer.Length);

            Buffer.BlockCopy(buffer, offset, _stateBuffer, _readOffset, bytesToCopy);
            _receiveBytesLeft -= bytesToCopy;
            bytesTransferred -= bytesToCopy;
            offset += bytesToCopy;

            if (_receiveBytesLeft == 0)
            {
                _readOffset = 0;
                return true;
            }


            _readOffset += bytesToCopy;
            return false;
        }

        private void DecodeFilters(string filters)
        {
            var startPos = 0;
            while (startPos < filters.Length)
            {
                var colonPos = filters.IndexOf(':');
                var endPos = filters.IndexOf(';', colonPos);
                if (endPos == -1)
                    endPos = filters.Length;

                Properties.Add(filters.Substring(startPos, colonPos - startPos),
                    filters.Substring(colonPos + 1, endPos - colonPos - 1));
                startPos = endPos + 1;
            }
        }

        private string EncodeFilters()
        {
            var sb = new StringBuilder();
            foreach (var property in _properties)
            {
                sb.Append(property.Key);
                sb.Append(':');
                sb.Append(Uri.EscapeDataString(property.Value));
                sb.Append(';');
            }
            return sb.ToString();
        }

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("MessageFrame[{0}]", SequenceNumber);
        }
    }

    public class BackTrackException : Exception
    {
        public BackTrackException(string backtrack, int offset)
        {
            Offset = offset;
        }

        public int Offset { get; set; }
    }
}