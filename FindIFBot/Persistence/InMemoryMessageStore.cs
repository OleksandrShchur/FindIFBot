using System.Collections.Concurrent;

namespace FindIFBot.Persistence
{
    /// <summary>
    /// In-memory <see cref="IMessageStore"/> kept for tests and non-durable scenarios.
    /// The production registration uses <see cref="DbMessageStore"/> so pending submissions
    /// survive restarts. Entries here are still evicted after <see cref="EntryLifetime"/>
    /// so memory cannot grow without bound.
    /// </summary>
    public class InMemoryMessageStore : IMessageStore
    {
        private static readonly TimeSpan EntryLifetime = TimeSpan.FromHours(48);

        private readonly ConcurrentDictionary<int, Entry> _store = new();

        public Task StoreAsync(StoredMessage message, CancellationToken cancellationToken = default)
        {
            EvictExpired();
            _store[message.MessageId] = new Entry(message, DateTime.UtcNow);
            return Task.CompletedTask;
        }

        public Task<StoredMessage?> TryGetAsync(int messageId, CancellationToken cancellationToken = default)
        {
            if (_store.TryGetValue(messageId, out var entry) && !entry.IsExpired)
            {
                return Task.FromResult<StoredMessage?>(entry.Message);
            }

            _store.TryRemove(messageId, out _);
            return Task.FromResult<StoredMessage?>(null);
        }

        public Task RemoveAsync(int messageId, CancellationToken cancellationToken = default)
        {
            _store.TryRemove(messageId, out _);
            return Task.CompletedTask;
        }

        private void EvictExpired()
        {
            foreach (var pair in _store)
            {
                if (pair.Value.IsExpired)
                {
                    _store.TryRemove(pair.Key, out _);
                }
            }
        }

        private sealed record Entry(StoredMessage Message, DateTime StoredAtUtc)
        {
            public bool IsExpired => DateTime.UtcNow - StoredAtUtc > EntryLifetime;
        }
    }
}
