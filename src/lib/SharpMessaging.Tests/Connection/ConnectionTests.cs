using System.Net.Sockets;
using System.Threading;
using FluentAssertions;
using NSubstitute;
using SharpMessaging.Connection;
using SharpMessaging.Frames;
using Xunit;

namespace SharpMessaging.Tests.Connection
{
    public class ConnectionTests
    {
        [Fact]
        public void assigns_identity_properly()
        {
            var service = Substitute.For<IExtensionService>();
            var mgr = new BufferManager(100, 10);

            var sut = new SharpMessaging.Connection.Connection("adam", service, true, mgr);

            sut.Identity.Should().Be("adam");
        }

        [Fact]
        public void uses_the_assigned_socket()
        {
            var service = Substitute.For<IExtensionService>();
            var mgr = new BufferManager(100, 10);
            var isDisconnected = false;
            using (var helper = new ClientServerHelper()) // wrap for cleanup
            {


                var sut = new SharpMessaging.Connection.Connection("adam", service, true, mgr);
                sut.Disconnected += (o,error) => isDisconnected = true;
                sut.Assign(helper.Server);
                helper.Client.Shutdown(SocketShutdown.Send);
                Thread.Sleep(100);

            }

            isDisconnected.Should().BeTrue();
        }

        [Fact]
        public void send_a_frame()
        {
            var service = Substitute.For<IExtensionService>();
            var mgr = new BufferManager(100, 10);
            var isDisconnected = false;
            using (var helper = new ClientServerHelper()) // wrap for cleanup
            {


                var sut = new SharpMessaging.Connection.Connection("adam", service, true, mgr);
                sut.Disconnected += (o,error) => isDisconnected = true;
                sut.Assign(helper.Server);
                sut.Send(new HandshakeFrame(){Identity = "A"});
                Thread.Sleep(100);

                byte[] buffer = new byte[65535];
                var bytesRead = helper.Client.Receive(buffer, SocketFlags.None);
                var frame = new HandshakeFrame();
                var offset = 0;
                int len = bytesRead;
                frame.Read(buffer, ref offset, ref len);
                frame.Identity.Should().Be("A");
            }

        }
        
    }
}
