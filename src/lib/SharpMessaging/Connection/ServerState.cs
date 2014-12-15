namespace SharpMessaging.Connection
{
    public enum ServerState
    {
        WaitingOnInitialHandshake,
        WaitingOnFinalHandshake,
        Ready,
        Error
    }
}