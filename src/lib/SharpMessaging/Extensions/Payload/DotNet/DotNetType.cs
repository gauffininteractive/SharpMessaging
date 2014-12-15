using System;

namespace SharpMessaging.Extensions.Payload.DotNet
{
    public class DotNetType
    {
        private readonly string _typeName;

        public DotNetType(string typeName)
        {
            _typeName = typeName;
        }

        public DotNetType(Type payloadDotNetType)
        {
            _typeName = payloadDotNetType.AssemblyQualifiedName;
        }


        public Type CreateType()
        {
            return Type.GetType(_typeName);
        }
    }
}