using System;
using FluentAssertions;
using SharpMessaging.Connection;
using Xunit;

namespace SharpMessaging.Tests.Connection
{
    public class CircularListQueueTests
    {
        [Fact]
        public void throws_exception_if_the_capacity_is_exceeded()
        {
            var sut = new CircularQueueList<int>(1);
            sut.Enqueue(10);

            Action actual = () => sut.Enqueue(10);

            actual.ShouldThrow<InvalidOperationException>().WithMessage("Queue is full");
        }

        [Fact]
        public void indexer_gives_correct_value()
        {
            var sut = new CircularQueueList<int>(1);
            
            sut.Enqueue(10);

            sut[0].Should().Be(10);
        }

        [Fact]
        public void indexer_gives_correct_value_when_wrapped()
        {
            var sut = new CircularQueueList<int>(2);
            sut.Enqueue(1);
            sut.Dequeue();
            sut.Enqueue(2);
            sut.Enqueue(3);

            sut[0].Should().Be(2);
            sut[1].Should().Be(3); //real index is 0
        }

        [Fact]
        public void wrap_on_dequeue()
        {
            var sut = new CircularQueueList<int>(2);
            sut.Enqueue(1);
            sut.Enqueue(2);
            sut.Dequeue();
            sut.Enqueue(3);
            sut.Dequeue();

            var actual = sut.Dequeue();

            actual.Should().Be(3);
        }

        [Fact]
        public void cant_dequeue_empty_list()
        {
            var sut = new CircularQueueList<int>(1);
            sut.Enqueue(1);
            sut.Dequeue();

            Action actual = () => sut.Dequeue();

            actual.ShouldThrow<InvalidOperationException>();
        }



        [Fact]
        public void Describe_what_the_test_proves()
        {
            

            var sut = new CircularQueueList<int>(5);
            sut.Enqueue(1);
            sut.Enqueue(2);
            sut.Enqueue(3);
            sut.Enqueue(4);
            sut[0].Should().Be(1);
            sut[3].Should().Be(4);
            sut.Dequeue().Should().Be(1);
            sut.Dequeue().Should().Be(2);
            sut.Enqueue(5);
            sut.Enqueue(6);
            sut[0].Should().Be(3);
            sut[3].Should().Be(6);
        }
    }
}
