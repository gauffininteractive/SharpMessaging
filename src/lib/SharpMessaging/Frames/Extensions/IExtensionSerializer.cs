namespace SharpMessaging.Frames.Extensions
{
    /// <summary>
    ///     Extension serializers do not serialize the entire frame, but only the parts after the common header fields.
    /// </summary>
    public interface IExtensionSerializer
    {
        /// <summary>
        ///     Result generated when the parser is done.
        /// </summary>
        object Result { get; }

        /// <summary>
        ///     Number of byt
        /// </summary>
        /// <param name="length"></param>
        void SetContentLength(int length);

        /// <summary>
        ///     Process incoming bytes
        /// </summary>
        /// <param name="buffer">buffer containing the bytes to process</param>
        /// <param name="offset">Index if the first byte to process</param>
        /// <param name="count">Amount of bytes that MUST be processed</param>
        /// <exception cref="ParseException">Invalid data or the specified amount is larger than the expected amount</exception>
        /// <remarks>
        ///     <para>
        ///         The extension payload might required that this method is invoked serveral times.
        ///     </para>
        /// </remarks>
        /// <returns>
        ///     Offset where the next serializer should start process (unless the offset is the same as amount of bytes
        ///     transferred)
        /// </returns>
        int Parse(byte[] buffer, int offset, int count);

        /// <summary>
        ///     Reset state so that we can start to decode another frame.
        /// </summary>
        void Reset();
    }
}