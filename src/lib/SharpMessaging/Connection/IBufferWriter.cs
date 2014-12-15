namespace SharpMessaging.Connection
{
    public interface IBufferWriter
    {
        /// <summary>
        ///     Reset state
        /// </summary>
        /// <param name="context"></param>
        void ResetWrite(WriterContext context);

        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="context">Used to enqueue bytes for delivery.</param>
        /// <returns><c>true</c> if more buffers can be appened.</returns>
        bool Write(WriterContext context);
    }
}