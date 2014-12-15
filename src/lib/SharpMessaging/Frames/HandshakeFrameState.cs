namespace SharpMessaging.Frames
{
    public enum HandshakeFrameState
    {
        VersionMajor,
        VersionMinor,
        Flags,
        IdentityLength,
        Identity,
        RequiredLength,
        Required,
        OptionalLength,
        Optional
    }
}