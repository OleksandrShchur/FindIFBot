using FindIFBot.EF.Entities;
using Microsoft.EntityFrameworkCore;

namespace FindIFBot.EF.Repositories
{
    public class UserRequestHistoryRepository : IUserRequestHistoryRepository
    {
        private readonly BotDbContext _db;

        public UserRequestHistoryRepository(BotDbContext db)
        {
            _db = db;
        }

        public async Task Add(UserRequest request)
        {
            _db.UserRequests.Add(request);
            await _db.SaveChangesAsync();
        }

        public async Task Update(UserRequest request)
        {
            _db.UserRequests.Update(request);
            await _db.SaveChangesAsync();
        }

        public async Task<List<UserRequest>> GetByUserId(long userId)
        {
            return await _db.UserRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();
        }

        public async Task<UserRequest?> GetById(Guid id)
        {
            return await _db.UserRequests
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<bool> HasHistory(long userId)
        {
            return await _db.UserRequests
                .AnyAsync(r => r.UserId == userId);
        }
    }
}