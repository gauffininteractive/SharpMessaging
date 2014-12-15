SharpMessaging
==============

Fast and easy messaging solution for .NET.

SharpMessaging can send about 100 000 .NET objects per second (serialized using JSON).

# Features

* Reliable option (guaranteed delivery of every message as long as the application is running)
* Persistance (guaranteed delivery, even if application is restarted)
* Fast (500 000 msgs/second without serialization)
* Easy to setup.


**Example client**

```csharp
internal class Program
{
	var registry = new ExtensionRegistry();
	registry.AddRequiredExtension(new JsonExtension());
	registry.AddOptionalExtension(new BatchAckExtension(){MessagesPerAck = 100});
	registry.AddRequiredExtension(new DotNetTypeExtension());

	var client = new ClientConnection("TestClient", registry);
	client.Start("127.0.0.1", 8334);
	client.Send(new MessageFrame("Hello world"));
}
```

**Example server**

```csharp
internal class Program
{
	private static void Main(string[] args)
	{
		var registry = new ExtensionRegistry();
		registry.AddOptionalExtension(new BatchAckExtension());
		registry.AddOptionalExtension(new SingleAckExtension());
		registry.AddRequiredExtension(new DotNetTypeExtension());
		registry.AddRequiredExtension(new JsonExtension());
		
		var server = new Server.Server(registry);
		server.FrameReceived = OnFrame;
		server.Start(8334);

		Console.ReadLine();
	}

	private static void OnFrame(ServerClient channel, MessageFrame message)
	{
		Console.WriteLine("Msg received: " + message.Payload);
	}
}
```
