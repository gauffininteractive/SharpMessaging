using System.Collections.Generic;
using SharpMessaging.Frames;

namespace SharpMessaging.Extensions
{
    public interface IExtensionRegistry
    {
        void AddRequiredExtension(IFrameExtension extension);
        void AddOptionalExtension(IFrameExtension extension);
        HandshakeExtension[] GetOptionalForHandshake();
        HandshakeExtension[] GetRequiredForHandshake();
        HandshakeExtension[] GetAllForHandshake();
        IList<IFrameExtension> GetAll(IEnumerable<string> allChosen);
        bool Exists(string name);
        IFrameExtension Get(string name);
    }
}