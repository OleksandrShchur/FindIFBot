using FindIFBot.Domain;
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
                .AsNoTracking()
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

        public async Task<bool> TryTransitionStatusAsync(
            long userId,
            int userMessageId,
            RequestStatus expectedStatus,
            RequestStatus newStatus,
            string? channelLink,
            CancellationToken cancellationToken = default)
        {
            // Single atomic UPDATE ... WHERE Status = expected. At most one concurrent caller
            // matches, so double-delivered callbacks cannot transition (and therefore act) twice.
            var affected = await _db.UserRequests
                .Where(r => r.UserId == userId
                            && r.UserMessageId == userMessageId
                            && r.Status == expectedStatus)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.Status, newStatus)
                    .SetProperty(r => r.ChannelLink, channelLink),
                    cancellationToken);

            return affected > 0;
        }

        public async Task SetChannelLinkAsync(
            long userId,
            int userMessageId,
            string channelLink,
            CancellationToken cancellationToken = default)
        {
            await _db.UserRequests
                .Where(r => r.UserId == userId
                            && r.UserMessageId == userMessageId
                            && r.Status == RequestStatus.Approved)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.ChannelLink, channelLink),
                    cancellationToken);
        }
    }
}
