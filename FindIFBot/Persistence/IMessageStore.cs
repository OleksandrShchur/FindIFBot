namespace FindIFBot.Persistence
{
    public interface IMessageStore
    {
        void Store(int messageId, StoredMessage message);
        bool TryGet(int messageId, out StoredMessage message);
        void Remove(int messageId);
    }
}
