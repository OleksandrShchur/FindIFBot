using FindIFBot.EF.Entities;
using Microsoft.EntityFrameworkCore;

namespace FindIFBot.EF.Repositories
{
    public class UserSessionRepository : IUserSessionRepository
    {
        private readonly BotDbContext _db;

        public UserSessionRepository(BotDbContext db)
        {
            _db = db;
        }

        public async Task<UserSession> GetAsync(long userId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

            return entity ?? new UserSession { UserId = userId };
        }

        public async Task SaveAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            var entity = await _db.UserSessions.FindAsync(new object[] { session.UserId }, cancellationToken);

            if (entity is null)
            {
                _db.UserSessions.Add(session);
            }
            else
            {
                _db.Entry(entity).CurrentValues.SetValues(session);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ResetAsync(long userId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.UserSessions.FindAsync(new object[] { userId }, cancellationToken);
            if (entity != null)
            {
                _db.UserSessions.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
