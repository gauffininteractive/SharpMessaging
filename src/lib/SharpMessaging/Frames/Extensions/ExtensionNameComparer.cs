using System.Collections.Generic;

namespace SharpMessaging.Frames.Extensions
{
    public class ExtensionNameComparer : IEqualityComparer<HandshakeExtension>
    {
        public bool Equals(HandshakeExtension x, HandshakeExtension y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(HandshakeExtension obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}