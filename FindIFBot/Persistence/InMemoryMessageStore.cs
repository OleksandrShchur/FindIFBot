using System.Collections.Concurrent;

namespace FindIFBot.Persistence
{
    public class InMemoryMessageStore : IMessageStore
    {
        private readonly ConcurrentDictionary<int, StoredMessage> _store = new();

        public void Store(int messageId, StoredMessage message)
        {
            _store[messageId] = message;
        }

        public bool TryGet(int messageId, out StoredMessage message)
        {
            return _store.TryGetValue(messageId, out message!);
        }

        public void Remove(int messageId)
        {
            _store.TryRemove(messageId, out _);
        }
    }
}
