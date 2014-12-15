using System;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting;

namespace SharpMessaging.BenchmarkApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var parser = new SimpleCommandLineParser();
            parser.Parse(args);

            if (parser.Arguments.ContainsKey("server"))
            {
                var server = new LoadTestServer
                {
                    MessagesPerAck = parser.GetValue("MessagesPerAck", 0, 0)
                };
                server.Start(parser.GetValue("server", 0, 8334));
                Console.WriteLine("Server started");
                if (parser.Arguments.ContainsKey("quit"))
                {
                    Console.WriteLine("Waiting for completion");
                    server.WaitForExit();
                    Console.WriteLine("Exiting");
                }
                else
                {
                    Console.WriteLine("Press ENTER to quit");
                    Console.ReadLine();
                }
            }
            else if (parser.Arguments.ContainsKey("client"))
            {
                Process serverProcess = null;
                if (parser.Arguments.ContainsKey("SpawnServer"))
                {
                    var hostAndPort = parser.Arguments["client"][0];
                    var port = hostAndPort.Split(':')[1];
                    var psi = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location);
                    psi.Arguments = " -server " + port + " -quit";
                    serverProcess = Process.Start(psi);
                }
                var client = new LoadTestClient
                {
                    MessageCount = parser.GetValue("MessageCount", 0, 10000),
                    MessageSize = parser.GetValue("MessageSize", 0, 1000),
                    MessagesPerAck = parser.GetValue("MessagesPerAck", 0, 0),
                    RemoteHost = parser.Arguments["client"][0]
                };

                var mbits = (client.MessageCount * client.MessageSize * 8) / 1000000;
                Console.WriteLine("Sending " + client.MessageCount + " messages of size " + client.MessageSize + " bytes. Total of " + mbits.ToString("N3") + "Mbits.");
                if (client.MessagesPerAck > 0)
                    Console.WriteLine("Using ACKs every " + client.MessagesPerAck + " message.");

                client.Start();
                Console.WriteLine("Waiting for exit");
                client.WaitForExit();
                if (serverProcess != null)
                {
                    client.Close();
                    Console.WriteLine("Waiting for server exit");
                    serverProcess.WaitForExit();
                }
            }
            else
            {
                Console.WriteLine("SharpMessaging.BenchmarkApp");
                Console.WriteLine();
                Console.WriteLine("SharpMessaging.BenchmarkApp -server [port]");
                Console.WriteLine("SharpMessaging.BenchmarkApp -client [host:port]");
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Options (for client and server)");
                Console.WriteLine("   Name             Default    Description");
                Console.WriteLine("   ==============================================");
                Console.WriteLine("   MessagesPerAck   0          0 = disable acks");
                Console.WriteLine();
                Console.WriteLine("Options (for client)");
                Console.WriteLine("   Name             Default    Description");
                Console.WriteLine("   ==============================================");
                Console.WriteLine("   MessageCount     10000      Number of messages to send");
                Console.WriteLine("   MessageSize      1000       Message size");
                Console.WriteLine();
                Console.WriteLine("Example:");
                Console.WriteLine("SharpMessaging.BenchmarkApp -client localhost:8334 -MessageCount 100000");
                return;
            }

        }
    }
}