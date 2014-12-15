using System;
using System.Net.Sockets;

namespace SharpMessaging.Connection
{
    public class DisconnectedEventArgs : EventArgs
    {
        public DisconnectedEventArgs(SocketError error)
        {
            Error = error;
        }

        public SocketError Error { get; private set; }
    }
}