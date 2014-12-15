using FluentAssertions;
using SharpMessaging.Connection;
using Xunit;

namespace SharpMessaging.Tests.Connection
{
    public class ByteBufferWriterTests
    {
        [Fact]
        public void can_write_to_our_context()
        {
            var buffer = new byte[] {1, 2, 3, 4, 5, 6, 7};
            var mgr = new BufferManager(100, 10);
            var context = new WriterContext(mgr);

            var sut = new ByteBufferWriter(buffer, 0, buffer.Length);
            sut.Write(context);

            context.GetPackets()[0].Buffer.Should().BeSubsetOf(buffer);
        }

        [Fact]
        public void can_do_partial_write()
        {
            var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            var mgr = new BufferManager(100, 10);
            var context = new WriterContext(mgr);
            context.BytesLeftToEnqueue = 4;

            var sut = new ByteBufferWriter(buffer, 0, buffer.Length);
            var actual = sut.Write(context);

            actual.Should().BeFalse();

        }

        [Fact]
        public void partial_bytes_are_copied()
        {
            var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            var mgr = new BufferManager(100, 10);
            var context = new WriterContext(mgr);
            context.BytesLeftToEnqueue = 4;

            var sut = new ByteBufferWriter(buffer, 0, buffer.Length);
            sut.Write(context);

            context.GetPackets()[0].Buffer[3].Should().Be(4);
        }

        [Fact]
        public void can_continue_on_a_partial_write()
        {
            var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            var mgr = new BufferManager(100, 10);
            var context = new WriterContext(mgr);
            context.BytesLeftToEnqueue = 4;

            var sut = new ByteBufferWriter(buffer, 0, buffer.Length);
            context.BytesLeftToEnqueue = 4;
            sut.Write(context);
            context.BytesLeftToEnqueue = 10;
            var actual = sut.Write(context);

            actual.Should().BeTrue();
            var packet = context.GetPackets()[1];
            packet.Buffer[packet.Offset + 2].Should().Be(7);
        }

    }
}
