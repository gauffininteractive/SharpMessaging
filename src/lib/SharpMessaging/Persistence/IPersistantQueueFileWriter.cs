namespace SharpMessaging.Persistence
{
    public interface IPersistantQueueFileWriter
    {
        long FileSize { get; }
        void Enqueue(byte[] data);
        void Enqueue(byte[] data, int offset, int length);
        void Open();
        void Close();
        void Flush();
    }
}