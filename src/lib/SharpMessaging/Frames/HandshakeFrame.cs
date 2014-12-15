using System;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using SharpMessaging.Connection;

namespace SharpMessaging.Frames
{
    /// <summary>
    ///     Parser and container for the client handshake.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         the extensions will be refered to by using a number.
    ///     </para>
    /// </remarks>
    public class HandshakeFrame : IFrame
    {
        private readonly byte[] _receiveBuffer = new byte[256];
        private int _receiveBufferOffset = 0;
        private int _receiveStateBytesLeft = 4;
        private int _receiveStateLength;
        private HandshakeFrameState _state;
        private ArraySegment<byte> _writeBuffer;

        public HandshakeFrame()
        {
            RequiredExtensions = new HandshakeExtension[0];
            OptionalExtensions = new HandshakeExtension[0];
            Identity = "";
        }

        public byte Flags { get; set; }
        public byte VersionMajor { get; set; }
        public byte VersionMinor { get; set; }
        public string Identity { get; set; }
        public HandshakeExtension[] RequiredExtensions { get; set; }
        public HandshakeExtension[] OptionalExtensions { get; set; }

        /// <summary>
        ///     Process bytes that we've received from the other end point. Might be a partial or complete frame.
        /// </summary>
        /// <param name="buffer">Buffer to process</param>
        /// <param name="offset">Where in buffer to start processing bytes</param>
        /// <param name="bytesToProcess">Bytes available to process</param>
        /// <returns>
        ///     Offset where the next serializer should start process (unless the offset is the same as amount of bytes
        ///     transferred)
        /// </returns>
        public bool Read(byte[] buffer, ref int offset, ref int bytesToProcess)
        {
            var numberOfBytesTransferredFromStart = bytesToProcess;
            var frameCompleted = false;
            while (bytesToProcess > 0 && !frameCompleted)
            {
                bool isBufferCopyCompleted;
                switch (_state)
                {
                    case HandshakeFrameState.VersionMajor:
                        VersionMajor = buffer[offset];
                        _state = HandshakeFrameState.VersionMinor;
                        ++offset;
                        --bytesToProcess;
                        break;

                    case HandshakeFrameState.VersionMinor:
                        VersionMinor = buffer[offset];
                        _state = HandshakeFrameState.Flags;
                        ++offset;
                        --bytesToProcess;
                        break;

                    case HandshakeFrameState.Flags:
                        Flags = buffer[offset];
                        _state = HandshakeFrameState.IdentityLength;
                        _receiveStateBytesLeft = 1;
                        ++offset;
                        --bytesToProcess;
                        break;

                    case HandshakeFrameState.IdentityLength:
                        _receiveStateLength = _receiveStateBytesLeft = buffer[offset];
                        _state = HandshakeFrameState.Identity;
                        ++offset;
                        --bytesToProcess;
                        break;

                    case HandshakeFrameState.Identity:
                        isBufferCopyCompleted = CopyToBuffer(buffer, ref offset, ref bytesToProcess);
                        if (isBufferCopyCompleted)
                        {
                            Identity = Encoding.ASCII.GetString(_receiveBuffer, 0, _receiveStateLength);
                            _receiveStateBytesLeft = 2;
                            _state = HandshakeFrameState.RequiredLength;
                        }
                        break;

                    case HandshakeFrameState.RequiredLength:
                        isBufferCopyCompleted = CopyToBuffer(buffer, ref offset, ref bytesToProcess);
                        if (isBufferCopyCompleted)
                        {
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(_receiveBuffer, 0, 2);
                            _receiveStateLength = _receiveStateBytesLeft = BitConverter.ToInt16(_receiveBuffer, 0);
                            if (_receiveStateLength == 0)
                            {
                                _state = HandshakeFrameState.OptionalLength;
                                _receiveStateBytesLeft = 2;
                            }
                            else
                            {
                                _state = HandshakeFrameState.Required;
                            }
                        }
                        break;
                    case HandshakeFrameState.Required:
                        isBufferCopyCompleted = CopyToBuffer(buffer, ref offset, ref bytesToProcess);
                        if (isBufferCopyCompleted)
                        {
                            var values = Encoding.ASCII.GetString(_receiveBuffer, 0, _receiveStateLength).Split(';');
                            RequiredExtensions = values.Select(HandshakeExtension.Parse).ToArray();
                            _receiveStateBytesLeft = 2;
                            _state = HandshakeFrameState.OptionalLength;
                        }
                        break;
                    case HandshakeFrameState.OptionalLength:
                        isBufferCopyCompleted = CopyToBuffer(buffer, ref offset, ref bytesToProcess);
                        if (isBufferCopyCompleted)
                        {
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(_receiveBuffer, 0, 2);
                            _receiveStateLength = _receiveStateBytesLeft = BitConverter.ToInt16(_receiveBuffer, 0);
                            if (_receiveStateLength == 0)
                                frameCompleted = true;
                            else
                                _state = HandshakeFrameState.Optional;
                        }
                        break;
                    case HandshakeFrameState.Optional:
                        isBufferCopyCompleted = CopyToBuffer(buffer, ref offset, ref bytesToProcess);
                        if (isBufferCopyCompleted)
                        {
                            var values = Encoding.ASCII.GetString(_receiveBuffer, 0, _receiveStateLength).Split(';');
                            OptionalExtensions = values.Select(HandshakeExtension.Parse).ToArray();
                            frameCompleted = true;
                        }
                        break;
                }
            }

            return frameCompleted;
        }

        public void ResetRead()
        {
            _state = HandshakeFrameState.VersionMajor;
            _receiveStateBytesLeft = 1; //identity length
            _receiveBufferOffset = 0;
            _receiveStateLength = -1;
            RequiredExtensions = new HandshakeExtension[0];
            OptionalExtensions = new HandshakeExtension[0];
            Identity = "";
        }

        /// <summary>
        ///     Connection have been lost. Reset state and return buffers.
        /// </summary>
        /// <param name="context"></param>
        public void ResetWrite(WriterContext context)
        {
            if (_writeBuffer.Count != 0)
            {
                context.ReturnBuffer(_writeBuffer);
                _writeBuffer = new ArraySegment<byte>();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="context">Used to enqueue bytes for delivery.</param>
        /// <returns><c>true</c> if more buffers can be appened.</returns>
        public bool Write(WriterContext context)
        {
            _writeBuffer = context.DequeueBuffer();
            var len = CopyTo(_writeBuffer.Array, _writeBuffer.Offset);
            context.Enqueue(new SendPacketsElement(_writeBuffer.Array, _writeBuffer.Offset, len));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CopyToBuffer(byte[] buffer, ref int offset, ref int bytesTransferred)
        {
            var bytesToCopy = Math.Min(_receiveStateBytesLeft, bytesTransferred);
            Buffer.BlockCopy(buffer, offset, _receiveBuffer, _receiveBufferOffset, bytesToCopy);

            _receiveStateBytesLeft -= bytesToCopy;
            bytesTransferred -= bytesToCopy;
            offset += bytesToCopy;

            if (_receiveStateBytesLeft == 0)
            {
                _receiveBufferOffset = 0;
                return true;
            }

            _receiveBufferOffset += bytesToCopy;
            return false;
        }

        public int CopyTo(byte[] destination, int offset)
        {
            var firstOffset = offset;
            destination[offset++] = VersionMajor;
            destination[offset++] = VersionMinor;
            destination[offset++] = Flags;

            //identity
            destination[offset++] = (byte) Identity.Length;
            _receiveStateBytesLeft = Encoding.ASCII.GetBytes(Identity, 0, Identity.Length, destination, offset);
            offset += _receiveStateBytesLeft;

            //required extensions
            if (RequiredExtensions == null || RequiredExtensions.Length == 0)
            {
                //leave them as zeroes.
                offset += 2;
            }
            else
            {
                var str = string.Join(";", RequiredExtensions.Select(x => x.Serialize()));
                var buf = BitConverter.GetBytes((ushort) str.Length);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(buf);
                Buffer.BlockCopy(buf, 0, destination, offset, 2);
                _receiveStateBytesLeft = Encoding.ASCII.GetBytes(str, 0, str.Length, destination, offset + 2);
                offset += 2 + _receiveStateBytesLeft;
            }

            //optional extensions
            if (OptionalExtensions == null || OptionalExtensions.Length == 0)
            {
                //leave them as zeroes.
                offset += 2;
            }
            else
            {
                var str = string.Join(";", OptionalExtensions.Select(x => x.Serialize()));
                var buf = BitConverter.GetBytes((ushort) str.Length);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(buf);
                Buffer.BlockCopy(buf, 0, destination, offset, 2);
                _receiveStateBytesLeft = Encoding.ASCII.GetBytes(str, 0, str.Length, destination, offset + 2);
                offset += 2 + _receiveStateBytesLeft;
            }

            return offset - firstOffset;
        }

        public HandshakeExtension GetExtension(string name)
        {
            if (name == null) throw new ArgumentNullException("name");

            return RequiredExtensions.FirstOrDefault(x => x.Name == name) ??
                   OptionalExtensions.FirstOrDefault(x => x.Name == name);
        }
    }
}