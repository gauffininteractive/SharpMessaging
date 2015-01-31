using System;

namespace SharpMessaging.Connection
{
    /// <summary>
    ///     Exception thrown at the other end point.
    /// </summary>
    public class RemoteEndPointException : Exception
    {
        public RemoteEndPointException(string errorMessage)
            : base(errorMessage)
        {
        }
    }
}