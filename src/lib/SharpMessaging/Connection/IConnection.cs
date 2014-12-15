using System.Net;
using System.Net.Sockets;
using SharpMessaging.Frames;

namespace SharpMessaging.Connection
{
    public interface IConnection
    {
        /// <summary>
        ///     Enqueue message and activate the internal send procedue
        /// </summary>
        /// <param name="frame"></param>
        void Send(IFrame frame);

        /// <summary>
        ///     Enqueue message, but do not initiate the Send procedure yet.
        /// </summary>
        /// <param name="frame"></param>
        void SendMore(IFrame frame);

        void Start(string remoteAddress, int port);
        void Start(IPAddress remoteAddress, int port);
        void Assign(Socket socket);
    }
}