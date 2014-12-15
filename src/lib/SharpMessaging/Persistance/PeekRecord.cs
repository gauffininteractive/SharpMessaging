namespace SharpMessaging.Persistance
{
    /// <summary>
    ///     A record that we have read from the file but not yet marked as read.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The purpose of this file is to reduce the number of IO operations as we do not move the file position back to
    ///         before the record (and thus reducing the number of reads required).
    ///     </para>
    /// </remarks>
    public class PeekRecord
    {
        /// <summary>
        /// </summary>
        /// <param name="position">Position in the file</param>
        /// <param name="recordSize">How large the data record is</param>
        /// <param name="buffer">Buffer containing the data record</param>
        public PeekRecord(long position, int recordSize, byte[] buffer)
        {
            Position = (int) position;
            RecordSize = recordSize;
            Buffer = buffer;
        }

        public int RecordSize { get; set; }
        public int Position { get; set; }
        public byte[] Buffer { get; set; }
    }
}