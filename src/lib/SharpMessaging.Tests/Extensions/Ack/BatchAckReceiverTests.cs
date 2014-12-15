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

            var sut = new BatchAckReceiver(connection, frame => deliveredFrame = frame, 5) { Threshold = 5 };
            sut.AddFrame(new MessageFrame{SequenceNumber = 1});

            deliveredFrame.Should().NotBeNull();
        }

        [Fact]
        public void deliver_last_frame_within_the_specified_ack_count()
        {
            var connection = Substitute.For<IConnection>();
            MessageFrame deliveredFrame = null;

            var sut = new BatchAckReceiver(connection, frame => deliveredFrame = frame, 5){Threshold = 5};
            sut.AddFrame(new MessageFrame { SequenceNumber = 1 });
            sut.AddFrame(new MessageFrame { SequenceNumber = 2 });
            sut.AddFrame(new MessageFrame { SequenceNumber = 3 });
            sut.AddFrame(new MessageFrame { SequenceNumber = 4 });
            sut.AddFrame(new MessageFrame { SequenceNumber = 5 });

            deliveredFrame.SequenceNumber.Should().Be(5);
        }

        [Fact]
        public void do_not_deliver_message_beoyend_the_specified_sequence()
        {
            var connection = Substitute.For<IConnection>();
            MessageFrame deliveredFrame = null;

            var sut = new BatchAckReceiver(connection, frame => deliveredFrame = frame, 4) { Threshold = 1 };
            sut.AddFrame(new MessageFrame { SequenceNumber = 1 });
            sut.AddFrame(new MessageFrame { SequenceNumber = 2 });

            deliveredFrame.SequenceNumber.Should().Be(1);
        }


        [Fact]
        public void Should_be_able_to_ack_simple_sequence()
        {
            var connection = Substitute.For<IConnection>();
            MessageFrame deliveredFrame = null;

            var sut = new BatchAckReceiver(connection, frame => deliveredFrame = frame, 4) { Threshold = 1 };
            sut.AddFrame(new MessageFrame{SequenceNumber = 1});
            sut.AddFrame(new MessageFrame { SequenceNumber = 2 });
            sut.Confirm(new AckFrame(1, 1));

            deliveredFrame.SequenceNumber.Should().Be(2);
        }
    }
}
