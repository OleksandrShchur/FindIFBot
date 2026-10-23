using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Repositories;
using Microsoft.Extensions.Options;

namespace FindIFBot.Services.Admin
{
    public interface IAdminQueueReminderScheduler
    {
        /// <summary>Schedules a due time when the first pending item enters an empty streak.</summary>
        Task OnPendingAddedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears or adjusts reminder state after a request leaves Pending.
        /// When <paramref name="wasApproved"/> and a reminder was already sent, resets due from now.
        /// </summary>
        Task OnPendingLeftAsync(bool wasApproved, CancellationToken cancellationToken = default);
    }

    public class AdminQueueReminderScheduler : IAdminQueueReminderScheduler
    {
        private readonly IAdminQueueReminderStateRepository _state;
        private readonly IUserRequestHistoryRepository _history;
        private readonly ReminderOptions _options;
        private readonly TimeProvider _timeProvider;

        public AdminQueueReminderScheduler(
            IAdminQueueReminderStateRepository state,
            IUserRequestHistoryRepository history,
            IOptions<ReminderOptions> options,
            TimeProvider timeProvider)
        {
            _state = state;
            _history = history;
            _options = options.Value;
            _timeProvider = timeProvider;
        }

        public async Task OnPendingAddedAsync(CancellationToken cancellationToken = default)
        {
            var existing = await _state.GetAsync(cancellationToken);
            if (existing?.DueAtUtc is not null)
                return;

            var dueAt = _timeProvider.GetUtcNow().UtcDateTime.Add(Delay);
            await _state.UpsertAsync(dueAt, existing?.LastSentAtUtc, cancellationToken);
        }

        public async Task OnPendingLeftAsync(bool wasApproved, CancellationToken cancellationToken = default)
        {
            if (!await _history.HasPendingAsync(cancellationToken))
            {
                await _state.ClearAsync(cancellationToken);
                return;
            }

            var existing = await _state.GetAsync(cancellationToken);
            if (existing is null)
                return;

            // Publish after at least one reminder this streak -> restart clock from publish time.
            if (wasApproved && existing.LastSentAtUtc is not null)
            {
                var dueAt = _timeProvider.GetUtcNow().UtcDateTime.Add(Delay);
                await _state.UpsertAsync(dueAt, existing.LastSentAtUtc, cancellationToken);
            }
            // Otherwise keep DueAtUtc (retarget to oldest at send time).
        }

        private TimeSpan Delay => TimeSpan.FromMinutes(Math.Max(1, _options.DelayMinutes));
    }
}
