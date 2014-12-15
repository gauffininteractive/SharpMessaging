namespace SharpMessaging.Frames
{
    public enum ExtensionFrameState
    {
        /// <summary>
        ///     Copy flags. 1 byte
        /// </summary>
        Flags,

        /// <summary>
        /// </summary>
        ExtensionId,

        /// <summary>
        ///     copy payload length. 1 byte or 4 bytes depending on the flag in "Flags"
        /// </summary>
        PayloadLength,

        /// <summary>
        ///     The parser in the extension
        /// </summary>
        Payload
    }
}