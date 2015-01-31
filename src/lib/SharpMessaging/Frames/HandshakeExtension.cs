using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpMessaging.Frames
{
    public class HandshakeExtension
    {
        public HandshakeExtension(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            Name = name;
            Properties = new Dictionary<string, string>();
        }

        public HandshakeExtension(string name, IDictionary<string, string> properties)
        {
            Name = name;
            Properties = properties;
        }

        public string Name { get; private set; }
        public IDictionary<string, string> Properties { get; private set; }

        protected bool Equals(HandshakeExtension other)
        {
            return string.Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public bool IsSameExtension(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            return Name.Equals(name);
        }

        public bool IsSameExtension(HandshakeExtension other)
        {
            if (other == null) throw new ArgumentNullException("other");
            return Name.Equals(other.Name);
        }

        public static HandshakeExtension Parse(string nameAndProperties)
        {
            var pos = nameAndProperties.IndexOf(':');
            if (pos == -1)
            {
                return new HandshakeExtension(nameAndProperties);
            }

            var extension = new HandshakeExtension(nameAndProperties.Substring(0, pos));
            var properties = nameAndProperties.Substring(pos + 1).Split(',');
            foreach (var property in properties)
            {
                pos = property.IndexOf('=');
                if (pos == -1)
                    throw new FormatException(
                        "Extension was not formatted correctly, missing equal sign for a property. Extension: " +
                        nameAndProperties);

                var key = property.Substring(0, pos);
                var value = Uri.UnescapeDataString(property.Substring(pos + 1));
                extension.Properties.Add(key, value);
            }
            return extension;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((HandshakeExtension) obj);
        }

        public string Serialize()
        {
            if (Properties.Any())
            {
                return Name + ":" +
                       string.Join(",", Properties.Select(x => x.Key + "=" + Uri.EscapeDataString(x.Value)));
            }

            return Name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}