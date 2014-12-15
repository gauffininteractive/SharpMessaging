using System;

namespace SharpMessaging.Connection
{
    public class FaultExceptionEventArgs : EventArgs
    {
        public FaultExceptionEventArgs(string errorMessage, Exception exception)
        {
            if (errorMessage == null) throw new ArgumentNullException("errorMessage");
            if (exception == null) throw new ArgumentNullException("exception");
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public string ErrorMessage { get; private set; }
        public Exception Exception { get; private set; }
    }
}