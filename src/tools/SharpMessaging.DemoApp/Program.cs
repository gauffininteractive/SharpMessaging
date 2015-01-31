using System;
using System.Text;
using SharpMessaging.Extensions;
using SharpMessaging.Extensions.Ack;
using SharpMessaging.Extensions.Payload.DotNet;
using SharpMessaging.fastJSON;
using SharpMessaging.Frames;
using SharpMessaging.Server;

namespace SharpMessaging.DemoApp
{
    class Program
    {
        private static void Main(string[] args)
        {
            var registry = new ExtensionRegistry();
            registry.AddOptionalExtension(new AckExtension()
            {
                MessagesPerAck = 200,
                AckExpireTime = TimeSpan.FromSeconds(1)
            });
            registry.AddOptionalExtension(new DotNetTypeExtension());
            registry.AddOptionalExtension(new FastJsonExtension());
            var server = new SharpMessagingServer(registry);
            server.FrameReceived = OnFrame;
            server.Start(8334);

            CreateClient();

            Console.ReadLine();
        }

        public static void CreateClient()
        {
            var registry = new ExtensionRegistry();
            registry.AddRequiredExtension(new AckExtension()
            {
                MessagesPerAck = 10,
                AckExpireTime = TimeSpan.FromSeconds(1)
            });
            //registry.AddRequiredExtension(new FastJsonExtension());
            var client = new SharpMessagingClient("MyClient", registry);
            client.Start("localhost", 8334);
            client.Send(new MessageFrame("Hello"));
            Console.WriteLine("Sent!");
            Console.ReadLine();
        }

        private static int batch;
        private static void OnFrame(ServerClient channel, MessageFrame frame)
        {
            var msg = Encoding.ASCII.GetString(frame.PayloadBuffer.Array, frame.PayloadBuffer.Offset,
                frame.PayloadBuffer.Count);

            Console.WriteLine("Received '" + msg + "' from " + channel.RemoteEndPoint);
        }
    }
}
