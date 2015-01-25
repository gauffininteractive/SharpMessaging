using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using SharpMessaging.Persistence;
using Xunit;

namespace SharpMessaging.Tests.Persistence
{
    public class PersistantQueueTests
    {
        [Fact]
        public void Peeking_from_an_empty_queue_should_return_zero_records()
        {
            var qfm = Substitute.For<IQueueFileManager>();
            var reader = Substitute.For<IPersistantQueueFileReader>();
            qfm.OpenCurrentReadFile().Returns(reader);
            var messages = new List<byte[]>();

            var sut = new PersistantQueue(qfm);
            sut.Open();
            sut.Peek(messages, 100);

            messages.Should().BeEmpty();
        }

        [Fact]
        public void Peeking_with_an_existing_message_should_return_it()
        {
            var qfm = Substitute.For<IQueueFileManager>();
            var reader = Substitute.For<IPersistantQueueFileReader>();
            var messages = new List<byte[]>();
            reader.Peek(messages, 100);
            qfm.OpenCurrentReadFile().Returns(reader);

            var sut = new PersistantQueue(qfm);
            sut.Open();
            sut.Peek(messages, 100);

            messages.Should().BeEmpty();
        }
    }
}
