using System.Collections.Generic;
using FluentAssertions;
using SharpMessaging.Connection;
using SharpMessaging.Frames;
using Xunit;

namespace SharpMessaging.Tests
{
    public class HandshakeFrameTests
    {
        [Fact]
        public void frame_Should_be_able_to_parse_multiple_incoming_buffers_if_reseted_in_between()
        {
            var bufferManager = new BufferManager(8192, 1);
            var context = new WriterContext(bufferManager);
            var buffer = bufferManager.Dequeue();
            bufferManager.Enqueue(buffer);
            var sut = new HandshakeFrame();
            sut.Identity = "Client";
            sut.OptionalExtensions = new[] {new HandshakeExtension("json"), new HandshakeExtension("ack") };
            sut.RequiredExtensions = new[] {new HandshakeExtension("dotnet") };
            sut.VersionMajor = 1;
            sut.Write(context);
            sut.ResetWrite(context);

            var offset = buffer.Offset;
            var len = context.GetPackets()[0].Count;
            sut.Read(buffer.Array, ref offset, ref len);
            sut.ResetRead();
            offset = buffer.Offset;
            len = context.GetPackets()[0].Count;
            sut.Read(buffer.Array, ref offset, ref len);

            len.Should().Be(0);
        }

        [Fact]
        public void frame_with_extension_properties_can_be_codec()
        {
            var bufferManager = new BufferManager(8192, 1);
            var context = new WriterContext(bufferManager);
            var buffer = bufferManager.Dequeue();
            bufferManager.Enqueue(buffer);
            var sut = new HandshakeFrame();
            sut.Identity = "Client";
            sut.OptionalExtensions = new[] { new HandshakeExtension("json", new Dictionary<string, string>{{"Key", "Value"}}), new HandshakeExtension("ack") };
            sut.RequiredExtensions = new[] { new HandshakeExtension("dotnet") };
            sut.VersionMajor = 1;
            sut.Write(context);
            sut.ResetWrite(context);

            var offset = buffer.Offset;
            var receiveFrame = new HandshakeFrame();
            var len = context.GetPackets()[0].Count;
            receiveFrame.Read(buffer.Array, ref offset, ref len);

            receiveFrame.OptionalExtensions[0].Properties["Key"].Should().Be("Value");
        }
    }
}
