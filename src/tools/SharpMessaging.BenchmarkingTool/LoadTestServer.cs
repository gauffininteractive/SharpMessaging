using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SharpMessaging.Extensions;
using SharpMessaging.Extensions.Ack;
using SharpMessaging.Extensions.Payload.DotNet;
using SharpMessaging.fastJSON;
using SharpMessaging.Frames;
using SharpMessaging.Server;

namespace SharpMessaging.BenchmarkApp
{
    internal class LoadTestServer
    {
        private int _batchCounter;
        ManualResetEvent _completedEvent = new ManualResetEvent(false);
        private bool _timeSyncCompleted;
        private SharpMessagingServer _server;
        public int MessagesPerAck { get; set; }

        public void Start(int port)
        {
            var registry = new ExtensionRegistry();
            registry.AddOptionalExtension(new BatchAckExtension()
            {
                MessagesPerAck = MessagesPerAck,
                AckExpireTime = TimeSpan.FromSeconds(1)
            });
            registry.AddOptionalExtension(new SingleAckExtension());
            registry.AddOptionalExtension(new DotNetTypeExtension());
            registry.AddOptionalExtension(new FastJsonExtension());
            _server = new SharpMessagingServer(registry);
            _server.FrameReceived = OnTimeSync;
            _server.ClientDisconnected = OnClientDisconnect;
            _server.Start(port);
        }

        private void OnClientDisconnect(ServerClient arg1, SocketError arg2)
        {
            Console.WriteLine("Disconnected");
            _timeSyncCompleted = false;
            _completedEvent.Set();
            _batchCounter = 0;
        }

        private void OnTimeSync(ServerClient channel, MessageFrame frame)
        {
            if (_timeSyncCompleted)
            {
                OnFrame(channel, frame);
                return;
            }
                
            var data = Encoding.ASCII.GetString(frame.PayloadBuffer.Array, frame.PayloadBuffer.Offset,
                frame.PayloadBuffer.Count);

            var parts = data.Split(';');
            var time = DateTime.Parse(parts[0]);

            var buffer =
                Encoding.ASCII.GetBytes((int)DateTime.UtcNow.Subtract(time).TotalMilliseconds + ";" +
                                        DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
            channel.Send(new MessageFrame(buffer));

            //completed time sync
            if (parts.Length == 2)
            {
                Console.WriteLine("Time synchronization completed");
                _timeSyncCompleted = true;
            }
                
        }

        private void OnFrame(ServerClient channel, MessageFrame frame)
        {
            if (frame.SequenceNumber == 65535)
            {
                _batchCounter ++;
                Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff") + " " + (_batchCounter*65535) + " messages.");
            }

            if (frame.Payload != null)
            {
                if (frame.Payload.ToString()[0] == 'm')
                {
                    Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff") + " done");
                    var buffer = Encoding.ASCII.GetBytes("completed");
                    channel.Send(new MessageFrame(buffer));
                }
                    
            }
            else if (frame.PayloadBuffer.Array[1] == 'm')
            {
                Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff") + " done");
                var buffer = Encoding.ASCII.GetBytes("completed");
                channel.Send(new MessageFrame(buffer));
            }
                
        }

        public void WaitForExit()
        {
            _completedEvent.WaitOne();
        }
    }
}