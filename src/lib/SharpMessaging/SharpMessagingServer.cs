using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using SharpMessaging.Connection;
using SharpMessaging.Extensions;
using SharpMessaging.Frames;
using SharpMessaging.Server;

namespace SharpMessaging
{
    public class SharpMessagingServer
    {
        public const byte Major = 1;
        public const byte Minor = 1;
        private readonly ConcurrentQueue<ServerClient> _availableClientPool = new ConcurrentQueue<ServerClient>();
        private readonly BufferManager _bufferManager = new BufferManager(65535, 10000);
        private readonly IExtensionRegistry _extensionProvider;
        public Action<ServerClient, SocketError> ClientDisconnected;
        public Action<ServerClient, MessageFrame> FrameReceived;
        private TcpListener _listener;

        public SharpMessagingServer(IExtensionRegistry extensionProvider)
        {
            _extensionProvider = extensionProvider;
            //ServerName = "FastSocket v" + Major + "." + Minor;
            ServerName = "SERVER";
        }

        public SharpMessagingServer()
            : this(new ExtensionRegistry())
        {
        }


        public string ServerName { get; set; }

        public void Start(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _listener.BeginAcceptSocket(OnAccept, null);
        }


        private void OnAccept(IAsyncResult ar)
        {
            try
            {
                var socket = _listener.EndAcceptSocket(ar);
                _listener.BeginAcceptSocket(OnAccept, null);

                ServerClient connection;
                if (!_availableClientPool.TryDequeue(out connection))
                {
                    Console.WriteLine("Allocating new client");
                    connection = new ServerClient(ServerName, _extensionProvider, _bufferManager)
                    {
                        FrameReceived = FrameReceived
                    };
                    connection.Disconnected += OnDisconnected;
                    connection.Start(socket);
                }
                else
                {
                    Console.WriteLine("Reusing client");
                    connection.Start(socket);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            var client = (ServerClient) sender;
            ClientDisconnected(client, e.Error);
            Console.WriteLine("Cleaning up client");
            client.Reset();
            _availableClientPool.Enqueue(client);
        }
    }
}