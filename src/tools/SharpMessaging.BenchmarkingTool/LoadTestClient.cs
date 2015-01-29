using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using SharpMessaging.Extensions;
using SharpMessaging.Extensions.Ack;
using SharpMessaging.Extensions.Payload.DotNet;
using SharpMessaging.Frames;

namespace SharpMessaging.BenchmarkApp
{
    internal class LoadTestClient
    {
        private readonly List<int> _timings = new List<int>();
        private SharpMessagingClient _client;
        ManualResetEvent _completedEvent = new ManualResetEvent(false);
        private TimeSpan _timeDifference;
        public int MessagesPerAck { get; set; }
        public string RemoteHost { get; set; }
        public int MessageSize { get; set; }
        public int MessageCount { get; set; }

        public DateTime Started { get; set; }

        public void Start()
        {
            var registry = new ExtensionRegistry();
            //registry.AddRequiredExtension(new JsonExtension());
            if (MessagesPerAck != 0)
            {
                registry.AddRequiredExtension(new AckExtension
                {
                    MessagesPerAck = MessagesPerAck,
                    AckExpireTime = TimeSpan.FromSeconds(120) // as we queue up msgs a lot faster than they can be sent.
                });
            }

            registry.AddRequiredExtension(new DotNetTypeExtension());
            var parts = RemoteHost.Split(':');
            var port = int.Parse(parts[1]);

            _client = new SharpMessagingClient("TestClient", registry);
            _client.Start(parts[0], port);
            _client.FrameReceived = OnTimingFrame;

            //start timing
            var buffer = Encoding.ASCII.GetBytes(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
            _client.Send(new MessageFrame(buffer));

            Console.WriteLine("All enqueued");
        }

        private void Benchmark(SharpMessagingClient client)
        {
            var buffer = MessageSize < 12
                ? Encoding.ASCII.GetBytes("1".PadLeft(MessageSize, '0'))
                : Encoding.ASCII.GetBytes("\"hello world ".PadRight(9999) + "\"");
            var payload = new ArraySegment<byte>(buffer);

            Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff") + " Starting");
            for (var i = 1; i < MessageCount; i++)
            {
                var frame = new MessageFrame
                {
                    PayloadBuffer = payload
                };
                client.Send(frame);
            }

            // packet to mark the end.
            client.Send(new MessageFrame
            {
                PayloadBuffer =
                    new ArraySegment<byte>(new[]
                    {(byte) '"', (byte) 'm', (byte) 'o', (byte) 't', (byte) 'h', (byte) 'e', (byte) 'r', (byte) '"'})
            });
        }

        private void OnTimingFrame(MessageFrame frame)
        {
            var data = Encoding.ASCII.GetString(frame.PayloadBuffer.Array, frame.PayloadBuffer.Offset,
                frame.PayloadBuffer.Count);

            var parts = data.Split(';');
            if (data == "completed")
            {
                var clockSyncAndNetworkDelay = TimeSpan.FromMilliseconds(_timings.Average());
                var elapsedTime = DateTime.UtcNow.Subtract(Started).Subtract(clockSyncAndNetworkDelay);
                var mbits = (MessageCount*MessageSize*8L/elapsedTime.TotalSeconds)/1000000;
                Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff") + " completed.");
                Console.WriteLine("Duration:      {0} ms", elapsedTime.TotalMilliseconds);
                Console.WriteLine("Message Size:  {0} bytes", MessageSize.ToString("N0"));
                Console.WriteLine("Message Count: {0}", MessageCount.ToString("N0"));
                Console.WriteLine("Total size:    {0} bytes", (MessageSize*MessageCount).ToString("N0"));
                Console.WriteLine("Msgs/sec:      {0}", (MessageCount/elapsedTime.TotalSeconds).ToString("N0"));
                Console.WriteLine("Throughput:    {0} Mbit/s", mbits.ToString("N1"));

                if (!File.Exists("result.csv"))
                {
                    File.AppendAllText("result.csv", "sep=,\r\n");
                    File.AppendAllText("result.csv", @"""Message size (bytes)"",""Message count"",""Transfer size (bytes)"",""Msgs/Ack"",""Duration (ms)"",""Msgs/sec"",""Troughput (Mbit/s)"""+"\r\n");
                }

                File.AppendAllText(@"result.csv",
                    string.Format("{0},{1},{2},{3},{4},{5},{6}\r\n",
                        MessageSize.ToString(CultureInfo.InvariantCulture),
                        MessageCount.ToString(CultureInfo.InvariantCulture),
                        (MessageSize * MessageCount).ToString(CultureInfo.InvariantCulture),
                        MessagesPerAck,
                        ((long)elapsedTime.TotalMilliseconds).ToString(CultureInfo.InvariantCulture),
                        ((long)(MessageCount/elapsedTime.TotalSeconds)).ToString(CultureInfo.InvariantCulture),
                        mbits.ToString(CultureInfo.InvariantCulture)));
                _completedEvent.Set();

                return;
            }

            var timing = int.Parse(parts[0]);
            _timings.Add(timing);
            var date = DateTime.Parse(parts[1], CultureInfo.InvariantCulture);
            var difference = DateTime.UtcNow.Subtract(date).TotalMilliseconds;
            _timings.Add((int) difference);

            if (_timings.Count == 10)
            {
                var buffer = Encoding.ASCII.GetBytes(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff") + ";end");
                _client.Send(new MessageFrame(buffer));
            }
            else if (_timings.Count == 12)
            {
                Started = DateTime.UtcNow;
                Benchmark(_client);
            }
            else
            {
                var buffer = Encoding.ASCII.GetBytes(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
                _client.Send(new MessageFrame(buffer));
            }
        }

        public void WaitForExit()
        {
            _completedEvent.WaitOne();
        }

        public void Close()
        {
            _client.Close();
        }
    }
}