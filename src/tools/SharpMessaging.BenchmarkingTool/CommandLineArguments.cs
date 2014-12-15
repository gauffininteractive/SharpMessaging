using System;
using clipr;

namespace SharpMessaging.BenchmarkApp
{
    public class CommandLineArgumentAttribute : Attribute
    {
        public char ShortName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string MetaValue { get; set; }
        public string DefaultValue { get; set; }
    }


    //[CommandLineArguments(Program = "SharpMessaging TestClient", Title = "SharpMessaging TestClient", Description = "Used to load test SharpMessaging")] 
    [ApplicationInfo(Description = "SharpMessaging TestClient.")]
    public class CommandLineArguments
    {
        [NamedArgument('h', "host", Description  = @"Connect to the specified server", MetaVar  = "localhost:10394")]
        public string RemoteHost { get; set; }

        [NamedArgument('s', "Server", Description  = @"Start a server on the specified port", MetaVar  = "10394",
            Const = 10394)]
        public int Port { get; set; }

        [NamedArgument('a', "MessagesPerAck", Description  = "Use acks. Specifies the amount of messages per ack",
            MetaVar = "200", Const = 0)]
        public int MessagesPerAck { get; set; }

        [NamedArgument('c', "Count", Description = "Amount of messages to send", Const = 10000, MetaVar = "10000")]
        public int Count { get; set; }

        [NamedArgument('s', "Size", Description = "Message size in bytes", Const = 100, MetaVar = "100")]
        public int Size { get; set; }
    }
}