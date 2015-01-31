using System;
using System.Collections.Generic;
using System.Linq;
using SharpMessaging.Connection;
using SharpMessaging.Extensions;

namespace SharpMessaging.Frames.Extensions
{
    /// <summary>
    ///     Used to manage extensions
    /// </summary>
    /// <remarks>
    ///     <para>This class is unique per connection.</para>
    /// </remarks>
    public class ExtensionService : IExtensionService
    {
        private readonly IExtensionRegistry _registry;
        private readonly Action<MessageFrame> _sendDirectly;
        private IList<IFrameExtension> _chosenExtensions = new List<IFrameExtension>();

        public ExtensionService(IExtensionRegistry registry, Action<MessageFrame> sendDirectly)
        {
            _registry = registry;
            _sendDirectly = sendDirectly;
        }

        /// <summary>
        ///     First step client side
        /// </summary>
        /// <param name="clientName"></param>
        /// <returns></returns>
        public HandshakeFrame CreateClientHandshake(string clientName)
        {
            return new HandshakeFrame
            {
                Identity = clientName,
                VersionMajor = SharpMessagingServer.Major,
                VersionMinor = SharpMessagingServer.Minor,
                OptionalExtensions = _registry.GetOptionalForHandshake(),
                RequiredExtensions = _registry.GetRequiredForHandshake()
            };
        }

        /// <summary>
        ///     First step at server side
        /// </summary>
        /// <param name="clientFrame"></param>
        /// <param name="serverName"></param>
        /// <returns></returns>
        public IFrame ServerNegotiate(HandshakeFrame clientFrame, string serverName)
        {
            var ourRequired = _registry.GetRequiredForHandshake();
            var missingExtensionsThatTheClientRequire =
                clientFrame.RequiredExtensions.Except(_registry.GetAllForHandshake(), new ExtensionNameComparer())
                    .ToList();
            if (missingExtensionsThatTheClientRequire.Any())
            {
                return
                    new ErrorFrame("Server to not support the following extensions: " +
                                   string.Join(", ", missingExtensionsThatTheClientRequire));
            }

            var missingExtensionsThatTheServerRequire =
                ourRequired.Except(clientFrame.RequiredExtensions.Union(clientFrame.OptionalExtensions),
                    new ExtensionNameComparer()).ToList();
            if (missingExtensionsThatTheServerRequire.Any())
            {
                return
                    new ErrorFrame("Server requires the following extensions: " +
                                   string.Join(", ", missingExtensionsThatTheServerRequire));
            }


            var required = ourRequired.Union(clientFrame.RequiredExtensions).Distinct().ToList();
            var chosenOptional =
                _registry.GetOptionalForHandshake()
                    .Union(clientFrame.OptionalExtensions)
                    .Distinct()
                    .Except(required, new ExtensionNameComparer());

            return new HandshakeFrame
            {
                Identity = serverName,
                VersionMajor = SharpMessagingServer.Major,
                VersionMinor = SharpMessagingServer.Minor,
                OptionalExtensions = chosenOptional.ToArray(),
                RequiredExtensions = required.ToArray()
            };
        }

        public IFrame CreateFrame(byte extensionId)
        {
            if (extensionId > _chosenExtensions.Count)
                throw new ArgumentOutOfRangeException("extensionId", extensionId,
                    "Not within a valid range, existing extensions: " + string.Join(", ", _chosenExtensions));

            return _chosenExtensions[extensionId - 1].CreateFrame(extensionId);
        }

        public IFrame CreateFrame(string name, object payload)
        {
            for (var i = 0; i < _chosenExtensions.Count; i++)
            {
                var extension = _chosenExtensions[i];
                if (extension.Name == name)
                    return extension.CreateFrame((byte) (i + 1), payload);
            }

            throw new ArgumentOutOfRangeException("name", name,
                "Extension has not been registered or chosen during the handshake.");
        }

        public void Reset()
        {
            _chosenExtensions.Clear();
        }

        /// <summary>
        ///     Final step client side.
        /// </summary>
        /// <param name="serverFrame"></param>
        /// <param name="clientName"></param>
        /// <returns></returns>
        public HandshakeFrame ClientConfirmExtensions(HandshakeFrame serverFrame, string clientName)
        {
            var extensions = _registry.GetRequiredForHandshake().ToList();
            var required = new List<HandshakeExtension>(extensions);
            foreach (var extension in serverFrame.RequiredExtensions)
            {
                if (!extensions.Contains(extension))
                    extensions.Add(extension);
                if (!required.Contains(extension))
                    required.Add(extension);
            }
            //do not use optional extensions. 
            // currently only use them to expose all extensions to the remote end point.

            //foreach (var extension in _registry.GetOptionalForHandshake())
            //{
            //    if (!extensions.Contains(extension))
            //        extensions.Add(extension);
            //}
            //foreach (var extension in serverFrame.OptionalExtensions)
            //{
            //    if (!extensions.Contains(extension) && _registry.Exists(extension.Name))
            //        extensions.Add(extension);
            //}

            //var optional = _registry.GetOptionalForHandshake().Union(serverFrame.OptionalExtensions).Distinct();


            _chosenExtensions = _registry.GetAll(extensions.Select(x => x.Name));

            var negotiatedRequired = new List<HandshakeExtension>();
            foreach (var handshakeExtension in required)
            {
                var serverExtension =
                    serverFrame.RequiredExtensions.FirstOrDefault(x => x.IsSameExtension(handshakeExtension));
                var clientExtension =
                    _registry.Get(handshakeExtension.Name);

                var extension = clientExtension.Negotiate(serverExtension);
                negotiatedRequired.Add(extension);
            }


            return new HandshakeFrame
            {
                Identity = clientName,
                VersionMajor = SharpMessagingServer.Major,
                VersionMinor = SharpMessagingServer.Minor,
                OptionalExtensions = new HandshakeExtension[0],
                RequiredExtensions = required.ToArray()
            };
        }

        public IFrameExtension Get(byte extensionIdentifier)
        {
            if (extensionIdentifier <= 0)
                throw new ArgumentOutOfRangeException("extensionIdentifier", extensionIdentifier, "Extensions have id:s that are 1 or larger.");
            return _chosenExtensions[extensionIdentifier - 1];
        }

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
        public void Prepare(MessageFrame frame, IConnection connection)
        {
        }

        /// <summary>
        ///     Final step at server side.
        /// </summary>
        /// <param name="clientFrame">Extensions that the client selected for this connection.</param>
        public void ServerConfirm(HandshakeFrame clientFrame)
        {
            var all = clientFrame.RequiredExtensions.Union(clientFrame.OptionalExtensions)
                .Distinct()
                .Select(x => x.Name);
            _chosenExtensions = _registry.GetAll(all);
        }


        /// <summary>
        ///     Return the topmost extension from a list (the one registered first is prefered).
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public string FindFirstExtensionNamed(params string[] names)
        {
            var name = _chosenExtensions.Select(x => x.Name).FirstOrDefault(names.Contains);
            if (name != null)
                return name;

            return null;
        }

        public byte FindFirstExtensionId(params string[] names)
        {
            for (var i = 0; i < _chosenExtensions.Count; i++)
            {
                if (names.Contains(_chosenExtensions[i].Name))
                    return (byte) (i + 1); //one based index
            }

            return 0;
        }
    }
}