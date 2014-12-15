using System;
using SharpMessaging.Frames;

namespace SharpMessaging.Connection
{
    public class HandshakeFrameReceivedEventArgs : EventArgs
    {
        public HandshakeFrameReceivedEventArgs(HandshakeFrame handshake)
        {
            if (handshake == null) throw new ArgumentNullException("handshake");
            Handshake = handshake;
        }

        public HandshakeFrame Handshake { get; private set; }
    }
}