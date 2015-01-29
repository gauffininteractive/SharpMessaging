namespace SharpMessaging.Persistence
{
    public interface IQueueItemSerializer
    {
        object Deserialize(byte[] buffer);
        byte[] Serialize(object message);
    }
}