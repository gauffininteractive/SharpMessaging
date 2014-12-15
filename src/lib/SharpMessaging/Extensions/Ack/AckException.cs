using System;

namespace SharpMessaging.Extensions.Ack
{
    public class AckException : Exception
    {
        public AckException(string errorMessage)
            : base(errorMessage)
        {
        }
    }
}