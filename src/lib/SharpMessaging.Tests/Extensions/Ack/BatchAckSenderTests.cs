using System;
using FluentAssertions;
using NSubstitute;
using SharpMessaging.Connection;
using SharpMessaging.Extensions.Ack;
using SharpMessaging.Frames;
using Xunit;

namespace SharpMessaging.Tests.Extensions.Ack
{
    public class BatchAckSenderTests
    {
        [Fact]
        public void do_not_send_an_ack_For_the_first_message()
        {
            var connection = Substitute.For<IConnection>();

            var sut = new AckSender(connection, 1) { Threshold = 10, TimeoutBeforeSendingAck = TimeSpan.FromDays(1) };
            sut.AckFrame(new MessageFrame { SequenceNumber = 1 });

            connection.DidNotReceiveWithAnyArgs().Send(null);
        }

        [Fact]
        public void send_an_ack_For_the_last_message_in_the_sequence()
        {
            var connection = Substitute.For<IConnection>();

            var sut = new AckSender(connection, 1) { Threshold = 2, TimeoutBeforeSendingAck = TimeSpan.FromDays(1) };
            sut.AckFrame(new MessageFrame { SequenceNumber = 1 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 21 });

            connection.ReceivedWithAnyArgs().Send(null);
        }


        [Fact]
        public void do_not_ack_When_Sequence_is_wrapping_but_still_within_the_limit()
        {
            var connection = Substitute.For<IConnection>();

            var sut = new AckSender(connection, 1) { Threshold = 3, TimeoutBeforeSendingAck = TimeSpan.FromDays(1) };
            sut.AckFrame(new MessageFrame { SequenceNumber = 65535 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 1 });

            connection.DidNotReceiveWithAnyArgs().Send(null);
        }


        [Fact]
        public void ack_When_Sequence_is_wrapping_and_over_the_specified_amount()
        {
            var connection = Substitute.For<IConnection>();

            var sut = new AckSender(connection, 1) { Threshold = 3, TimeoutBeforeSendingAck = TimeSpan.FromDays(1) };
            sut.AckFrame(new MessageFrame { SequenceNumber = 65535 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 1 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 2 });

            connection.ReceivedWithAnyArgs().Send(null);
        }

        [Fact]
        public void ReAck_if_wrapped_scope()
        {
            var connection = Substitute.For<IConnection>();

            var sut = new AckSender(connection, 1) { Threshold = 3, TimeoutBeforeSendingAck = TimeSpan.FromDays(1) };
            sut.AckFrame(new MessageFrame { SequenceNumber = 65533 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65534 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65535 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 1 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 2 });
            var actual = sut.ShouldReAck(new MessageFrame { SequenceNumber = 65534 });

            actual.Should().BeTrue();
        }

        [Fact]
        public void do_not_ReAck_very_old_messages()
        {
            //TODO: Kill the connection
        }

        [Fact]
        public void do_not_reack_within_the_wrapped_sequence()
        {
            var connection = Substitute.For<IConnection>();

            var sut = new AckSender(connection, 1) { Threshold = 3, TimeoutBeforeSendingAck = TimeSpan.FromDays(1) };
            sut.AckFrame(new MessageFrame { SequenceNumber = 65533 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65534 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65535 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 1 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 2 });
            var actual = sut.ShouldReAck(new MessageFrame { SequenceNumber = 3 });

            actual.Should().BeFalse();
        }

        [Fact]
        public void do_not_reack_when_Sequence_wraps()
        {
            var connection = Substitute.For<IConnection>();

            var sut = new AckSender(connection, 1) { Threshold = 3, TimeoutBeforeSendingAck = TimeSpan.FromDays(1) };
            sut.AckFrame(new MessageFrame { SequenceNumber = 65531 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65532 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65533 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65534 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65535 });
            var actual = sut.ShouldReAck(new MessageFrame { SequenceNumber = 1 });

            actual.Should().BeFalse();
        }

        [Fact]
        public void do_not_reack_when_Sequence_wraps_and_the_threshold_is_not_fulfilled()
        {
            var connection = Substitute.For<IConnection>();

            var sut = new AckSender(connection, 1) { Threshold = 3, TimeoutBeforeSendingAck = TimeSpan.FromDays(1) };
            sut.AckFrame(new MessageFrame { SequenceNumber = 65530 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65531 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65532 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65533 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65534 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65535 });
            var actual = sut.ShouldReAck(new MessageFrame { SequenceNumber = 1 });

            actual.Should().BeFalse();
        }

        [Fact]
        public void do_not_reack_within_the_sequence()
        {
            var connection = Substitute.For<IConnection>();

            var sut = new AckSender(connection, 1) { Threshold = 3, TimeoutBeforeSendingAck = TimeSpan.FromDays(1) };
            sut.AckFrame(new MessageFrame { SequenceNumber = 65533 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65534 });
            var actual = sut.ShouldReAck(new MessageFrame { SequenceNumber = 65535 });

            actual.Should().BeFalse();
        }


        [Fact]
        public void do_not_reack_after_the_sequence()
        {
            var connection = Substitute.For<IConnection>();

            var sut = new AckSender(connection, 1) { Threshold = 3, TimeoutBeforeSendingAck = TimeSpan.FromDays(1) };
            sut.AckFrame(new MessageFrame { SequenceNumber = 65533 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 65534 });
            var actual = sut.ShouldReAck(new MessageFrame { SequenceNumber = 2 });

            actual.Should().BeFalse();
        }

        [Fact]
        public void do_not_reack_long_after_the_sequence()
        {
            var connection = Substitute.For<IConnection>();

            var sut = new AckSender(connection, 1) { Threshold = 3, TimeoutBeforeSendingAck = TimeSpan.FromDays(1) };
            sut.AckFrame(new MessageFrame { SequenceNumber = 1 });
            sut.AckFrame(new MessageFrame { SequenceNumber = 2 });
            var actual = sut.ShouldReAck(new MessageFrame { SequenceNumber = 200 });

            actual.Should().BeFalse();
        }
    }
}
