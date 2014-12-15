using System;
using System.Net;
using System.Net.Sockets;

namespace SharpMessaging.Tests.Connection
{
    public class ClientServerHelper : IDisposable
    {
        public ClientServerHelper()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var ar = listener.BeginAcceptSocket(null, null);

            Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Client.Connect(IPAddress.Loopback, ((IPEndPoint) listener.LocalEndpoint).Port);

            Server = listener.EndAcceptSocket(ar);
            listener.Stop();
        }

        public Socket Server { get; set; }

        public Socket Client { get; set; }

        public void Dispose()
        {
        }
    }
}