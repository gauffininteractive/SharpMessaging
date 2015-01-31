using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using SharpMessaging.Connection;
using SharpMessaging.Frames;
using Xunit;

namespace SharpMessaging.Tests.Frames
{
    public class ErrorFrame
    {
        [Fact]
        public void serialize_and_deserialize_should_be_compatible()
        {
            var mgr = new BufferManager(65535, 10);
            var ctx = new WriterContext(mgr);
            var msg1 =new SharpMessaging.Frames.ErrorFrame("Help!");
            msg1.Write(ctx);

            var buf = ctx.GetPackets()[0].Buffer;
            var offset = ctx.GetPackets()[0].Offset;
            var len = ctx.GetPackets()[0].Count;
            var msg2 = new SharpMessaging.Frames.ErrorFrame();
            msg2.Read(buf, ref offset, ref len);

            msg2.ErrorMessage.Should().Be("Help!");
        }
    }
}
