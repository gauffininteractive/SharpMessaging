using SharpMessaging.Connection;

namespace SharpMessaging.Frames
{
    /// <summary>
    ///     Defines a class which can serialize a frame
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The implementations do not need to be thread safe
    ///     </para>
    /// </remarks>
    public interface IFrame : IBufferWriter
    {
        /// <summary>
        ///     Connection failed. Reset state until  a new connection is received.
        /// </summary>
        void ResetRead();

        /// <summary>
        ///     Process bytes that we've received from the other end point. Might be a partial or complete frame.
        /// </summary>
        /// <param name="buffer">Buffer to process</param>
        /// <param name="offset">Where in buffer to start processing bytes</param>
        /// <param name="bytesToProcess">Bytes available to process</param>
        /// <returns>
        ///     Offset where the next serializer should start process (unless the offset is the same as amount of bytes
        ///     transferred)
        /// </returns>
        bool Read(byte[] buffer, ref int offset, ref int bytesToProcess);
    }
}