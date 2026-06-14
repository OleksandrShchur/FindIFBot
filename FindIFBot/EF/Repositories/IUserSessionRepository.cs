using FindIFBot.EF.Entities;

namespace FindIFBot.EF.Repositories
{
    public interface IUserSessionRepository
    {
        Task<UserSession> GetAsync(long userId, CancellationToken cancellationToken = default);
        Task SaveAsync(UserSession session, CancellationToken cancellationToken = default);
        Task ResetAsync(long userId, CancellationToken cancellationToken = default);
    }
}
