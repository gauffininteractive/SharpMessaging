using System;
using FluentAssertions;
using SharpMessaging.Connection;
using Xunit;

namespace SharpMessaging.Tests.Connection
{
    public class BufferManagerTests
    {
        [Fact]
        public void can_dequeue_first_buffer()
        {
            var sut = new BufferManager(10, 100);

            var buf = sut.Dequeue();

            buf.Offset.Should().Be(0);
            buf.Count.Should().Be(10);
        }

        [Fact]
        public void can_return_buffer()
        {
            var sut = new BufferManager(10, 2);
            var buf = sut.Dequeue();

            sut.Enqueue(buf);

            sut.Dequeue();
            sut.Dequeue();
        }

        [Fact]
        public void can_dequeue_last_buffer()
        {
            var sut = new BufferManager(10, 2);
            sut.Dequeue();

            var buf = sut.Dequeue();

            buf.Offset.Should().Be(10);
            buf.Count.Should().Be(10);
        }

        [Fact]
        public void cannot_Return_someone_elses_buffer()
        {
            var sut = new BufferManager(10, 2);

            Action actual = () => sut.Enqueue(new ArraySegment<byte>());

            actual.ShouldThrow<ArgumentException>();
        }
    }
    
}
