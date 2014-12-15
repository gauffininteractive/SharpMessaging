using FluentAssertions;
using SharpMessaging.Frames;
using Xunit;

namespace SharpMessaging.Tests
{
    
    public class ClientHandshakeTests
    {
        [Fact]
        public void got_required_but_no_optional()
        {
            var buffer = new byte[]
            {
                3, //major
                1, //minor
                0, //flags
                4, //id length
                (byte) 'd', (byte) 'a', (byte) 'y', (byte) '1', //id
                0, 4, //required len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) 'n', //required,
                0, 0 //optional length
            };
            var offset = 0;
            var count = buffer.Length;

            var sut = new HandshakeFrame();
            var actual = sut.Read(buffer, ref offset, ref count);

            actual.Should().Be(true);
            sut.Flags.Should().Be(0);
            sut.Identity.Should().Be("day1");
            sut.RequiredExtensions[0].Should().Be("json");
            sut.OptionalExtensions.Should().BeEmpty();
        }

        [Fact]
        public void got_neither_optional_nor_required()
        {
            var buffer = new byte[]
            {
                3, //major
                1, //minor
                0, //flags
                4, //id length
                (byte) 'd', (byte) 'a', (byte) 'y', (byte) '1', //id
                0, 0, //required len (network byte order)
                0, 0 //optional length
            };
            var offset = 0;
            var count = buffer.Length;

            var sut = new HandshakeFrame();
            var actual = sut.Read(buffer, ref offset, ref count);

            actual.Should().Be(true);
            sut.Flags.Should().Be(0);
            sut.Identity.Should().Be("day1");
            sut.RequiredExtensions.Should().BeEmpty();
            sut.OptionalExtensions.Should().BeEmpty();
        }

        [Fact]
        public void got_only_optional()
        {
            var buffer = new byte[]
            {
                3, //major
                1, //minor
                0, //flags
                4, //id length
                (byte) 'd', (byte) 'a', (byte) 'y', (byte) '1', //id
                0, 0, //required len (network byte order)
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) 'n', //optional,
            };
            var offset = 0;
            var count = buffer.Length;

            var sut = new HandshakeFrame();
            var actual = sut.Read(buffer, ref offset, ref count);

            actual.Should().Be(true);
            sut.Flags.Should().Be(0);
            sut.Identity.Should().Be("day1");
            sut.RequiredExtensions.Should().BeEmpty();
            sut.OptionalExtensions[0].Should().Be("json");
        }

        [Fact]
        public void got_only_optional_and_required()
        {
            var buffer = new byte[]
            {
                3, //major
                1, //minor
                0, //flags
                4, //id length
                (byte) 'd', (byte) 'a', (byte) 'y', (byte) '1', //id
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) '1', //optional,
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) 'n', //optional,
            };
            var offset = 0;
            var count = buffer.Length;

            var sut = new HandshakeFrame();
            var actual = sut.Read(buffer, ref offset, ref count);

            actual.Should().Be(true);
            sut.Flags.Should().Be(0);
            sut.Identity.Should().Be("day1");
            sut.RequiredExtensions[0].Should().Be("jso1");
            sut.OptionalExtensions[0].Should().Be("json");
        }

        [Fact]
        public void got_only_major_in_first_send()
        {
            var buffer = new byte[]
            {
                3, //major
                1, //minor
                0, //flags
                4, //id length
                (byte) 'd', (byte) 'a', (byte) 'y', (byte) '1', //id
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) '1', //optional,
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) 'n', //optional,
            };
            var offset = 0;
            var count = 1;

            var sut = new HandshakeFrame();
            var actual1 = sut.Read(buffer, ref offset, ref count);
            count = buffer.Length - 1;
            var actual2 = sut.Read(buffer, ref offset, ref count);

            actual1.Should().Be(false);
            actual2.Should().Be(true);
            sut.OptionalExtensions[0].Should().Be("json");
        }

        [Fact]
        public void got_up_to_flags_in_first_send()
        {
            int breakPoint = 3;
            var buffer = new byte[]
            {
                3, //major
                1, //minor
                0, //flags
                4, //id length
                (byte) 'd', (byte) 'a', (byte) 'y', (byte) '1', //id
                0, 4, //required len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) '1', //required
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) 'n', //optional,
            };
            var offset = 0;
            var count1 = breakPoint;
            var count2 = buffer.Length - breakPoint;

            var sut = new HandshakeFrame();
            sut.Read(buffer, ref offset, ref count1);
            sut.Read(buffer, ref breakPoint, ref count2);

            sut.OptionalExtensions[0].Should().Be("json");
        }

        [Fact]
        public void got_half_id__in_first_send()
        {
            int breakPoint = 6;
            var buffer = new byte[]
            {
                3, //major
                1, //minor
                0, //flags
                4, //id length
                (byte) 'd', (byte) 'a', (byte) 'y', (byte) '1', //id
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) '1', //optional,
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) 'n', //optional,
            };
            var offset = 0;
            var count1 = breakPoint;
            var count2 = buffer.Length - breakPoint;

            var sut = new HandshakeFrame();
            sut.Read(buffer, ref offset, ref count1);
            sut.Read(buffer, ref breakPoint, ref count2);

            sut.OptionalExtensions[0].Should().Be("json");
        }

        [Fact]
        public void got_half__requird_length_in_first_send()
        {
            int breakPoint = 9;
            var buffer = new byte[]
            {
                3, //major
                1, //minor
                0, //flags
                4, //id length
                (byte) 'd', (byte) 'a', (byte) 'y', (byte) '1', //id
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) '1', //optional,
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) 'n', //optional,
            };
            var offset = 0;
            var count1 = breakPoint;
            var count2 = buffer.Length - breakPoint;

            var sut = new HandshakeFrame();
            sut.Read(buffer, ref offset, ref count1);
            sut.Read(buffer, ref breakPoint, ref count2);

            sut.OptionalExtensions[0].Should().Be("json");
        }

        [Fact]
        public void got_half_required_text_in_first_send()
        {
            int breakPoint = 12;
            var buffer = new byte[]
            {
                3, //major
                1, //minor
                0, //flags
                4, //id length
                (byte) 'd', (byte) 'a', (byte) 'y', (byte) '1', //id
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) '1', //optional,
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) 'n', //optional,
            };
            var offset = 0;
            var count1 = breakPoint;
            var count2 = buffer.Length - breakPoint;

            var sut = new HandshakeFrame();
            sut.Read(buffer, ref offset, ref count1);
            sut.Read(buffer, ref breakPoint, ref count2);

            sut.OptionalExtensions[0].Should().Be("json");
        }

        [Fact]
        public void got_half_optional_length_in_first_send()
        {
            int breakPoint = 15;
            var buffer = new byte[]
            {
                3, //major
                1, //minor
                0, //flags
                4, //id length
                (byte) 'd', (byte) 'a', (byte) 'y', (byte) '1', //id
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) '1', //optional,
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) 'n', //optional,
            };
            var offset = 0;
            var count1 = breakPoint;
            var count2 = buffer.Length - breakPoint;

            var sut = new HandshakeFrame();
            sut.Read(buffer, ref offset, ref count1);
            sut.Read(buffer, ref breakPoint, ref count2);

            sut.OptionalExtensions[0].Should().Be("json");
        }

        [Fact]
        public void got_half_optional_text_in_first_send()
        {
            int breakPoint = 17;
            var buffer = new byte[]
            {
                3, //major
                1, //minor
                0, //flags
                4, //id length
                (byte) 'd', (byte) 'a', (byte) 'y', (byte) '1', //id
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) '1', //optional,
                0, 4, //optional len (network byte order)
                (byte) 'j', (byte) 's', (byte) 'o', (byte) 'n', //optional,
            };
            var offset = 0;
            var count1 = breakPoint;
            var count2 = buffer.Length - breakPoint;

            var sut = new HandshakeFrame();
            sut.Read(buffer, ref offset, ref count1);
            sut.Read(buffer, ref breakPoint, ref count2);

            sut.OptionalExtensions[0].Should().Be("json");
        }
    }
}
