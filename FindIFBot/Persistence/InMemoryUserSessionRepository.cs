using System.Collections.Concurrent;
using FindIFBot.Domain;

namespace FindIFBot.Persistence
{
    public class InMemoryUserSessionRepository : IUserSessionRepository
    {
        private readonly ConcurrentDictionary<long, UserSession> _sessions = new();

        public UserSession Get(long userId)
        {
            return _sessions.GetOrAdd(userId, id => new UserSession { UserId = id });
        }

        public void Save(UserSession session)
        {
            _sessions[session.UserId] = session;
        }

        public void Reset(long userId)
        {
            _sessions.TryRemove(userId, out _);
        }
    }
}
