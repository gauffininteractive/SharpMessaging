using System;

namespace SharpMessaging.Frames
{
    [Flags]
    public enum FrameFlags : byte
    {
        None = 0,
        CommandFrame = 1,
        ExtensionFrame = 2,
        ErrorFrame = 4,
        LargeFrame = 8,
        Continued = 16
    }
}