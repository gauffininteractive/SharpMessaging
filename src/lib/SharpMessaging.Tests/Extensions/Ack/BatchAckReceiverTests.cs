using System;
using FluentAssertions;
using NSubstitute;
using SharpMessaging.Connection;
using SharpMessaging.Extensions.Ack;
using SharpMessaging.Frames;
using Xunit;

namespace SharpMessaging.Tests.Extensions.Ack
{
    public class BatchAckReceiverTests
    {
        [Fact]
        public void deliver_first_frame_within_the_specified_ack_count()
        {
            var connection = Substitute.For<IConnection>();
            MessageFrame deliveredFrame = null;

            var sut = new AckReceiver(connection, frame => deliveredFrame = frame, 5);
            sut.Send(new MessageFrame { SequenceNumber = 1 });

            deliveredFrame.Should().NotBeNull();
        }

        [Fact]
        public void deliver_last_frame_within_the_specified_ack_count()
        {
            var connection = Substitute.For<IConnection>();
            MessageFrame deliveredFrame = null;

            var sut = new AckReceiver(connection, frame => deliveredFrame = frame, 5);
            sut.Send(new MessageFrame { SequenceNumber = 1 });
            sut.Send(new MessageFrame { SequenceNumber = 2 });
            sut.Send(new MessageFrame { SequenceNumber = 3 });
            sut.Send(new MessageFrame { SequenceNumber = 4 });
            sut.Send(new MessageFrame { SequenceNumber = 5 });

            deliveredFrame.SequenceNumber.Should().Be(5);
        }

        [Fact]
        public void throw_if_trying_to_send_more_messages_than_can_be_acked()
        {
            var connection = Substitute.For<IConnection>();
            MessageFrame deliveredFrame = null;

            var sut = new AckReceiver(connection, frame => deliveredFrame = frame, 1);
            sut.Send(new MessageFrame { SequenceNumber = 1 });
            Action actual = () => sut.Send(new MessageFrame { SequenceNumber = 2 });

            actual.ShouldThrow<AckException>();
        }


        [Fact]
        public void Should_be_able_to_ack_simple_sequence()
        {
            var connection = Substitute.For<IConnection>();
            MessageFrame deliveredFrame = null;

            var sut = new AckReceiver(connection, frame => deliveredFrame = frame, 4);
            sut.Send(new MessageFrame { SequenceNumber = 1 });
            sut.Send(new MessageFrame { SequenceNumber = 2 });
            sut.Confirm(new AckFrame(1, 1));

            deliveredFrame.SequenceNumber.Should().Be(2);
        }
    }
}
