using System;
using SharpMessaging.Connection;

namespace SharpMessaging.Frames
{
    /// <summary>
    ///     USed to keep track of all
    /// </summary>
    public interface IExtensionService
    {
        /// <summary>
        ///     Final step client side.
        /// </summary>
        /// <param name="serverFrame"></param>
        /// <param name="clientName"></param>
        /// <returns></returns>
        HandshakeFrame ClientConfirmExtensions(HandshakeFrame serverFrame, string clientName);

        /// <summary>
        ///     First step client side
        /// </summary>
        /// <param name="clientName"></param>
        /// <returns></returns>
        HandshakeFrame CreateClientHandshake(string clientName);

        IFrameExtension Get(byte extensionIdentifier);

        byte FindFirstExtensionId(params string[] names);

        /// <summary>
        ///     Return the topmost extension from a list (the one registered first is prefered).
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        string FindFirstExtensionNamed(params string[] names);

        /// <summary>
        ///     Prepare connection for the next frame
        /// </summary>
        /// <param name="frame">SEND frame that will be distributed to the client</param>
        /// <param name="connection">Connection to send extension frames on</param>
        /// <remarks>
        ///     <para>
        ///         Prepare means that any extension frames that might be required for the next message should be enqueued for
        ///         delivery (using <c>connection.SendMore()</c>).
        ///     </para>
        /// </remarks>
        void Prepare(MessageFrame frame, IConnection connection);

        /// <summary>
        ///     Final step at server side.
        /// </summary>
        /// <param name="handshakeFrame"></param>
        void ServerConfirm(HandshakeFrame handshakeFrame);

        /// <summary>
        ///     First step at server side
        /// </summary>
        /// <param name="clientFrame"></param>
        /// <param name="serverName"></param>
        /// <returns></returns>
        IFrame ServerNegotiate(HandshakeFrame clientFrame, string serverName);

        /// <summary>
        ///     Create a new frame
        /// </summary>
        /// <param name="extensionId">Id used to identify the extension frame</param>
        /// <returns>Frame of the specified type.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Specified extension is not registered</exception>
        IFrame CreateFrame(byte extensionId);

        /// <summary>
        ///     Create a new frame and assign the payload directly.
        /// </summary>
        /// <param name="name">Extension name</param>
        /// <param name="payload">Payload to attach</param>
        /// <returns>Frame of the specified type.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Specified extension is not registered</exception>
        IFrame CreateFrame(string name, object payload);

        /// <summary>
        ///     Connection got disconnected, prepare for reuse.
        /// </summary>
        void Reset();
    }
}