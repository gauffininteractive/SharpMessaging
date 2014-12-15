namespace SharpMessaging.Frames
{
    public enum MessageFrameState
    {
        /// <summary>
        ///     Copy flags. 1 byte
        /// </summary>
        Flags,

        /// <summary>
        ///     copy sequence number, 2 bytes
        /// </summary>
        SequenceNumber,

        DestinationLength,
        Destination,

        FilterLength,
        Filter,

        /// <summary>
        ///     copy payload length. 1 byte or 4 bytes depending on the flag in "Flags"
        /// </summary>
        PayloadLength,

        /// <summary>
        ///     Payload is large, handle it accordingly
        /// </summary>
        LargePayload,

        /// <summary>
        ///     Payload is small (use an internal buffer)
        /// </summary>
        SmallPayload
    }
}