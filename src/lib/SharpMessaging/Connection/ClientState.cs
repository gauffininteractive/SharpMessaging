namespace SharpMessaging.Connection
{
    public enum ClientState
    {
        ClientToServerHandshake1,
        ServerToClientHandshake,
        ClientToServerHandshake2,
        Ready,
        Error
    }
}