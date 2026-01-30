using FindIFBot.EF.Entities;

namespace FindIFBot.EF.Repositories
{
    public interface IUserSessionRepository
    {
        UserSession Get(long userId);
        void Save(UserSession session);
        void Reset(long userId);
    }
}
