using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using SharpMessaging.Connection;
using SharpMessaging.Frames;
using Xunit;

namespace SharpMessaging.Tests
{
    public class MessageFrameTests
    {
        [Fact]
        public void code_decode_test()
        {
            var bufMgr = new BufferManager(65535, 1);
            var context = new WriterContext(bufMgr);
            var buf = bufMgr.Dequeue();
            bufMgr.Enqueue(buf);

            var sut = new MessageFrame();
            sut.SequenceNumber = 22;
            var payload = Encoding.UTF8.GetBytes("Hello world");
            sut.PayloadBuffer = new ArraySegment<byte>(payload, 0, payload.Length);
            sut.Write(context);

            var pos = 0;
            var len = context.GetPackets().First().Count;
            var frame2 = new MessageFrame();
            frame2.Read(buf.Array, ref pos, ref len);
            frame2.SequenceNumber.Should().Be(22);
            Encoding.ASCII.GetString(sut.PayloadBuffer.Array, 0, sut.PayloadBuffer.Count).Should().Be("Hello world");
        }

        [Fact]
        public void the_10kb_payload()
        {
            var bufMgr = new BufferManager(65535, 1);
            var context = new WriterContext(bufMgr);
            var buf = bufMgr.Dequeue();
            bufMgr.Enqueue(buf);

            var sut = new MessageFrame();
            sut.SequenceNumber = 22;
            var payload = Encoding.ASCII.GetBytes("Hello world".PadRight(10000));
            sut.PayloadBuffer = new ArraySegment<byte>(payload, 0, payload.Length);
            sut.Write(context);

            var pos = 0;
            var len = context.GetPackets().First().Count;
            var frame2 = new MessageFrame();
            frame2.Read(buf.Array, ref pos, ref len);
            frame2.SequenceNumber.Should().Be(22);
            frame2.IsFlaggedAsSmall.Should().BeFalse();
            Encoding.ASCII.GetString(sut.PayloadBuffer.Array, 0, sut.PayloadBuffer.Count).TrimEnd(' ').Should().Be("Hello world");
        }


        [Fact]
        public void code_decode_with_properties_test()
        {
            var bufMgr = new BufferManager(65535, 1);
            var context = new WriterContext(bufMgr);
            var buf = bufMgr.Dequeue();
            bufMgr.Enqueue(buf);

            var sut = new MessageFrame();
            sut.SequenceNumber = 22;
            sut.Properties.Add("hello", "world");
            var payload = Encoding.UTF8.GetBytes("Hello world");
            sut.PayloadBuffer = new ArraySegment<byte>(payload, 0, payload.Length);
            sut.Write(context);

            var pos = 0;
            var len = context.GetPackets().First().Count;
            var frame2 = new MessageFrame();
            frame2.Read(buf.Array, ref pos, ref len);
            frame2.SequenceNumber.Should().Be(22);
            Encoding.ASCII.GetString(sut.PayloadBuffer.Array, 0, sut.PayloadBuffer.Count).Should().Be("Hello world");
            sut.Properties["hello"].Should().Be("world");
        }

        [Fact]
        public void got_no_place_to_go()
        {
            var buffer = new byte[]
            {
                0, //flags,
                0, 2, // sequence number,
                0, //destination length
                0, 0, // filter length
                6, //payload length
                (byte) 'm', (byte) 'o', (byte) 't', (byte) 'h', (byte)'e', (byte)'r' //payload
            };
            var offset = 0;
            var count = buffer.Length;

            var sut = new MessageFrame();
            var actual = sut.Read(buffer, ref offset, ref count);

            actual.Should().Be(true);
            sut.Flags.Should().Be(FrameFlags.None);
            sut.SequenceNumber.Should().Be(2);
            sut.Destination.Should().BeEmpty();
            Encoding.ASCII.GetString(sut.PayloadBuffer.Array, 0, sut.PayloadBuffer.Count).Should().Be("mother");
        }

        [Fact]
        public void got_no_filter()
        {
            var buffer = new byte[]
            {
                0, //flags,
                0, 2, // sequence number,
                2, //destination length,
                (byte)'M', (byte)'Q',
                0, 0, // filter length
                6, //payload length
                (byte) 'm', (byte) 'o', (byte) 't', (byte) 'h', (byte)'e', (byte)'r' //payload
            };
            var offset = 0;
            var count = buffer.Length;

            var sut = new MessageFrame();
            var actual = sut.Read(buffer, ref offset, ref count);

            sut.Destination.Should().Be("MQ");
            sut.Properties.Should().BeEmpty();
        }

        [Fact]
        public void deserualize_Simplistic_filter()
        {
            var filter = Encoding.ASCII.GetBytes("last_name<kalle");
            var buffer = new byte[]
            {
                0, //flags,
                0, 2, // sequence number,
                2, //destination length,
                (byte)'M', (byte)'Q',
                0, 15, // filter length
                (byte)'l', (byte)'a', (byte)'s', (byte)'t', (byte)'_', (byte)'n', (byte)'a', (byte)'m', (byte)'e', //filter part 1
                (byte)':', (byte)'k', (byte)'a', (byte)'l', (byte)'l', (byte)'e', //filter part 2
                6, //payload length
                (byte) 'm', (byte) 'o', (byte) 't', (byte) 'h', (byte)'e', (byte)'r' //payload
            };
            var offset = 0;
            var count = buffer.Length;

            var sut = new MessageFrame();
            var actual = sut.Read(buffer, ref offset, ref count);

            actual.Should().Be(true);
            sut.Destination.Should().Be("MQ");
            sut.Properties["last_name"].Should().Be("kalle");
        }

        [Fact]
        public void serialize_Simplistic_filter()
        {
            var bufferManager = new BufferManager(8192, 1);
            var context = new WriterContext(bufferManager);
            var buffer = bufferManager.Dequeue();
            bufferManager.Enqueue(buffer);
            var expected = new byte[]
            {
                0, //flags,
                0, 2, // sequence number,
                2, //destination length,
                (byte)'M', (byte)'Q',
                0, 16, // filter length
                (byte)'l', (byte)'a', (byte)'s', (byte)'t', (byte)'_', (byte)'n', (byte)'a', (byte)'m', (byte)'e', //filter part 1
                (byte)':', (byte)'k', (byte)'a', (byte)'l', (byte)'l', (byte)'e', (byte)';', //filter part 2
                6, //payload length
                (byte) 'm', (byte) 'o', (byte) 't', (byte) 'h', (byte)'e', (byte)'r' //payload
            };

            var sut = new MessageFrame();
            sut.SequenceNumber = 2;
            sut.Destination = "MQ";
            sut.Properties.Add("last_name", "kalle");
            var payload = Encoding.ASCII.GetBytes("mother");
            sut.PayloadBuffer = new ArraySegment<byte>(payload, 0, payload.Length);
            var actual = sut.Write(context);

            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] != buffer.Array[i])
                    throw new InvalidOperationException("Differs at " + i);
            }
        }

        [Fact]
        public void got_destination()
        {
            var buffer = new byte[]
            {
                0, //flags,
                0, 2, // sequence number,
                2, //destination length,
                (byte)'M', (byte)'Q',
                0, 0, // filter length
                6, //payload length
                (byte) 'm', (byte) 'o', (byte) 't', (byte) 'h', (byte)'e', (byte)'r' //payload
            };
            var offset = 0;
            var count = buffer.Length;

            var sut = new MessageFrame();
            var actual = sut.Read(buffer, ref offset, ref count);

            sut.Destination.Should().Be("MQ");
            Encoding.ASCII.GetString(sut.PayloadBuffer.Array, 0, sut.PayloadBuffer.Count).Should().Be("mother");
        }

        [Fact]
        public void automatically_allocate_larger_payload_buffer()
        {
            var unix = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var now = new DateTime(2014, 10, 13, 13, 0, 0, DateTimeKind.Local).ToUniversalTime();
            var seconds = now.Subtract(unix).TotalSeconds;

            byte[] length = BitConverter.GetBytes(4096);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(length);
            var buffer = new byte[4096 + 12];
            buffer[0] = (byte)FrameFlags.LargeFrame; //flags,
            buffer[1] = 0; //sequence
            buffer[2] = 2;
            buffer[3] = 2;//destination length
            buffer[4] = (byte)'M';
            buffer[5] = (byte)'Q';
            buffer[6] = 0; // filter length
            buffer[7] = 0; // filter length
            buffer[8] = length[0];//payload length
            buffer[9] = length[1];
            buffer[10] = length[2];
            buffer[11] = length[3];
            var offset = 0;
            var count = buffer.Length;

            var sut = new MessageFrame();
            var actual = sut.Read(buffer, ref offset, ref count);

            actual.Should().Be(true);
            sut.PayloadBuffer.Array.Length.Should().BeGreaterThan(4096);
        }

        [Fact]
        public void doing_a_partial_payload_write_properly()
        {
            var bufMgr = new BufferManager(5010, 1);
            var context = new WriterContext(bufMgr);
            var buf1 = bufMgr.Dequeue();
            bufMgr.Enqueue(buf1);
            var sut = new MessageFrame();
            sut.SequenceNumber = 22;
            var payload = Encoding.ASCII.GetBytes("Hello world".PadRight(10000));
            sut.PayloadBuffer = new ArraySegment<byte>(payload, 0, payload.Length);

            
            var actual1 = sut.Write(context);
            var pos = 0;
            var len = context.GetPackets().First().Count;
            var frame2 = new MessageFrame();
            frame2.Read(buf1.Array, ref pos, ref len);

            var actual2 = sut.Write(context);
            pos = 0;
            len = context.GetPackets().Last().Count;
            frame2.Read(buf1.Array, ref pos, ref len);


            frame2.SequenceNumber.Should().Be(22);
            frame2.IsFlaggedAsSmall.Should().BeFalse();
            Encoding.ASCII.GetString(sut.PayloadBuffer.Array, 0, sut.PayloadBuffer.Count).TrimEnd(' ').Should().Be("Hello world");
            actual1.Should().BeFalse();
            actual2.Should().BeTrue();
        }
    }
}
