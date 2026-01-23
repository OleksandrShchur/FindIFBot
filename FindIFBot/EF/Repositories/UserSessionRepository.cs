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

        public UserSession Get(long userId)
        {
            var entity = _db.UserSessions
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == userId);

            return entity ?? new UserSession { UserId = userId };
        }

        public void Save(UserSession session)
        {
            var entity = _db.UserSessions.Find(session.UserId);

            if (entity is null)
            {
                _db.UserSessions.Add(session);
            }
            else
            {
                _db.Entry(entity).CurrentValues.SetValues(session);
            }

            _db.SaveChanges();
        }

        public void Reset(long userId)
        {
            var entity = _db.UserSessions.Find(userId);
            if (entity != null)
            {
                _db.UserSessions.Remove(entity);
                _db.SaveChanges();
            }
        }
    }
}
