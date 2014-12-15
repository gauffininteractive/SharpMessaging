using System;
using SharpMessaging.Connection;
using SharpMessaging.Extensions;

namespace SharpMessaging.Frames
{
    public class ExtensionFrameProcessor
    {
        private readonly Func<byte, IFrame> _extensionFactory;
        private readonly Action<ExtensionFrame> _frameReceived;
        private ExtensionFrame _receiveFrame;
        private ExtensionFrameState _receiveState = ExtensionFrameState.Flags;

        public ExtensionFrameProcessor(Func<byte, IFrame> extensionFactory, Action<ExtensionFrame> frameReceived)
        {
            if (extensionFactory == null) throw new ArgumentNullException("extensionFactory");
            if (frameReceived == null) throw new ArgumentNullException("frameReceived");
            _extensionFactory = extensionFactory;
            _frameReceived = frameReceived;
        }

        public FrameFlags Flags { get; set; }


        /// <summary>
        ///     Connection failed. Reset state until  a new connection is received.
        /// </summary>
        public void ResetRead()
        {
        }

        public bool Read(byte[] buffer, ref int offset, ref int bytesTransferred)
        {
            while (bytesTransferred > 0)
            {
                switch (_receiveState)
                {
                    case ExtensionFrameState.Flags:
                        Flags = (FrameFlags) buffer[offset];
                        _receiveState = ExtensionFrameState.ExtensionId;
                        ++offset;
                        --bytesTransferred;
                        break;
                    case ExtensionFrameState.ExtensionId:
                        var extensionId = buffer[offset];
                        ++offset;
                        --bytesTransferred;
                        _receiveState = ExtensionFrameState.Payload;
                        _receiveFrame = (ExtensionFrame) _extensionFactory(extensionId);
                        break;

                    case ExtensionFrameState.Payload:
                        var isCompleted = _receiveFrame.Read(buffer, ref offset, ref bytesTransferred);
                        if (isCompleted)
                        {
                            _frameReceived(_receiveFrame);
                            _receiveState = ExtensionFrameState.Flags;
                        }

                        return isCompleted;
                }
            }

            return false;
        }


        /// <summary>
        ///     Connection have been lost. Reset state and return buffers.
        /// </summary>
        /// <param name="context"></param>
        public void ResetWrite(WriterContext context)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="context">Used to enqueue bytes for delivery.</param>
        /// <returns><c>true</c> if more buffers can be appened.</returns>
        public bool Write(WriterContext context)
        {
            return false;
        }
    }
}