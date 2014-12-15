using System.Collections.Generic;
using System.Linq;
using SharpMessaging.Frames;

namespace SharpMessaging.Extensions
{
    public class ExtensionRegistry : IExtensionRegistry
    {
        private readonly List<IFrameExtension> _allAvailableExtensions = new List<IFrameExtension>();
        private readonly List<IFrameExtension> _optionalForHandshake = new List<IFrameExtension>();
        private readonly List<IFrameExtension> _requiredForHandshake = new List<IFrameExtension>();


        public void AddRequiredExtension(IFrameExtension extension)
        {
            _allAvailableExtensions.Add(extension);
            _requiredForHandshake.Add(extension);
        }

        public void AddOptionalExtension(IFrameExtension extension)
        {
            _allAvailableExtensions.Add(extension);
            _optionalForHandshake.Add(extension);
        }

        public HandshakeExtension[] GetOptionalForHandshake()
        {
            return _optionalForHandshake.Select(x => x.CreateHandshakeInfo()).ToArray();
        }

        public HandshakeExtension[] GetRequiredForHandshake()
        {
            return _requiredForHandshake.Select(x => x.CreateHandshakeInfo()).ToArray();
        }

        public HandshakeExtension[] GetAllForHandshake()
        {
            return _allAvailableExtensions.Select(x => x.CreateHandshakeInfo()).ToArray();
        }

        public IList<IFrameExtension> GetAll(IEnumerable<string> allChosen)
        {
            return allChosen.Select(ext => _allAvailableExtensions.First(x => x.Name == ext)).ToList();
        }

        public bool Exists(string name)
        {
            return _allAvailableExtensions.Any(x => x.Name == name);
        }

        public IFrameExtension Get(string name)
        {
            return _allAvailableExtensions.FirstOrDefault(x => x.Name == name);
        }
    }
}