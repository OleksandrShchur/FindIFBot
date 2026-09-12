using FindIFBot.EF.Entities;

namespace FindIFBot.EF.Repositories
{
    public interface IAdminQueueReminderStateRepository
    {
        Task<AdminQueueReminderState?> GetAsync(CancellationToken cancellationToken = default);
        Task UpsertAsync(DateTime? dueAtUtc, DateTime? lastSentAtUtc, CancellationToken cancellationToken = default);
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
