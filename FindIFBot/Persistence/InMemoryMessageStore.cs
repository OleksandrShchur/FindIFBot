using System.Collections.Concurrent;

namespace FindIFBot.Persistence
{
    /// <summary>
    /// In-memory store for messages that are buffered between submission and moderation.
    /// Entries are evicted both explicitly (after moderation) and automatically once they
    /// exceed <see cref="EntryLifetime"/>. The time-based eviction bounds memory usage so a
    /// flood of submissions whose moderation never completes (e.g. abandoned requests, lost
    /// callbacks) cannot grow the dictionary without limit.
    /// NOTE: this remains process-local and is lost on restart. Durable pending content
    /// should be persisted in the database — see the refactoring roadmap.
    /// </summary>
    public class InMemoryMessageStore : IMessageStore
    {
        // Generous enough not to interfere with realistic moderation turnaround,
        // while still guaranteeing the store cannot grow unbounded.
        private static readonly TimeSpan EntryLifetime = TimeSpan.FromHours(48);

        private readonly ConcurrentDictionary<int, Entry> _store = new();

        public void Store(int messageId, StoredMessage message)
        {
            EvictExpired();
            _store[messageId] = new Entry(message, DateTime.UtcNow);
        }

        public bool TryGet(int messageId, out StoredMessage message)
        {
            if (_store.TryGetValue(messageId, out var entry) && !entry.IsExpired)
            {
                message = entry.Message;
                return true;
            }

            // Drop the stale entry if it was found but expired.
            _store.TryRemove(messageId, out _);
            message = null!;
            return false;
        }

        public void Remove(int messageId)
        {
            _store.TryRemove(messageId, out _);
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
