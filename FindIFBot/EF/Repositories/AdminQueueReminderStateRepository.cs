using FindIFBot.EF.Entities;
using Microsoft.EntityFrameworkCore;

namespace FindIFBot.EF.Repositories
{
    public class AdminQueueReminderStateRepository : IAdminQueueReminderStateRepository
    {
        private readonly BotDbContext _db;

        public AdminQueueReminderStateRepository(BotDbContext db)
        {
            _db = db;
        }

        public async Task<AdminQueueReminderState?> GetAsync(CancellationToken cancellationToken = default)
        {
            return await _db.AdminQueueReminderStates
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == AdminQueueReminderState.SingletonId, cancellationToken);
        }

        public async Task UpsertAsync(
            DateTime? dueAtUtc,
            DateTime? lastSentAtUtc,
            CancellationToken cancellationToken = default)
        {
            var existing = await _db.AdminQueueReminderStates
                .FirstOrDefaultAsync(s => s.Id == AdminQueueReminderState.SingletonId, cancellationToken);

            if (existing is null)
            {
                _db.AdminQueueReminderStates.Add(new AdminQueueReminderState
                {
                    Id = AdminQueueReminderState.SingletonId,
                    DueAtUtc = dueAtUtc,
                    LastSentAtUtc = lastSentAtUtc
                });
            }
            else
            {
                existing.DueAtUtc = dueAtUtc;
                existing.LastSentAtUtc = lastSentAtUtc;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            await UpsertAsync(dueAtUtc: null, lastSentAtUtc: null, cancellationToken);
        }
    }
}
