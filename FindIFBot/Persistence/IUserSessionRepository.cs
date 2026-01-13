using FindIFBot.Domain;

namespace FindIFBot.Persistence
{
    public interface IUserSessionRepository
    {
        UserSession Get(long userId);
        void Save(UserSession session);
        void Reset(long userId);
    }
}
