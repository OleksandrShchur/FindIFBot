using FindIFBot.Configuration;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Services.Admin;
using Microsoft.Extensions.Options;

namespace FindIFBot.UnitTests.Services.Admin
{
    public class AdminQueueReminderSchedulerTests
    {
        private readonly IAdminQueueReminderStateRepository _state = Substitute.For<IAdminQueueReminderStateRepository>();
        private readonly IUserRequestHistoryRepository _history = Substitute.For<IUserRequestHistoryRepository>();
        private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 7, 28, 10, 0, 0, TimeSpan.Zero));

        private AdminQueueReminderScheduler CreateSut(int delayMinutes = 60) =>
            new(
                _state,
                _history,
                Options.Create(new ReminderOptions { DelayMinutes = delayMinutes }),
                _time);

        [Fact]
        public async Task OnPendingAdded_WhenNoDue_SchedulesFromNowPlusDelay()
        {
            _state.GetAsync().Returns((AdminQueueReminderState?)null);

            await CreateSut(delayMinutes: 60).OnPendingAddedAsync();

            await _state.Received(1).UpsertAsync(
                new DateTime(2026, 7, 28, 11, 0, 0, DateTimeKind.Utc),
                null,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task OnPendingAdded_WhenDueAlreadySet_DoesNotReschedule()
        {
            _state.GetAsync().Returns(new AdminQueueReminderState
            {
                DueAtUtc = new DateTime(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc)
            });

            await CreateSut().OnPendingAddedAsync();

            await _state.DidNotReceiveWithAnyArgs().UpsertAsync(default, default, default);
        }

        [Fact]
        public async Task OnPendingAdded_UsesConfiguredDelayMinutes()
        {
            _state.GetAsync().Returns((AdminQueueReminderState?)null);

            await CreateSut(delayMinutes: 15).OnPendingAddedAsync();

            await _state.Received(1).UpsertAsync(
                new DateTime(2026, 7, 28, 10, 15, 0, DateTimeKind.Utc),
                null,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task OnPendingLeft_WhenQueueEmpty_ClearsState()
        {
            _history.HasPendingAsync().Returns(false);

            await CreateSut().OnPendingLeftAsync(wasApproved: true);

            await _state.Received(1).ClearAsync(Arg.Any<CancellationToken>());
            await _state.DidNotReceiveWithAnyArgs().UpsertAsync(default, default, default);
        }

        [Fact]
        public async Task OnPendingLeft_PublishBeforeReminder_KeepsDue()
        {
            _history.HasPendingAsync().Returns(true);
            _state.GetAsync().Returns(new AdminQueueReminderState
            {
                DueAtUtc = new DateTime(2026, 7, 28, 11, 0, 0, DateTimeKind.Utc),
                LastSentAtUtc = null
            });

            await CreateSut().OnPendingLeftAsync(wasApproved: true);

            await _state.DidNotReceiveWithAnyArgs().ClearAsync(default);
            await _state.DidNotReceiveWithAnyArgs().UpsertAsync(default, default, default);
        }

        [Fact]
        public async Task OnPendingLeft_PublishAfterReminder_ResetsDueFromNow()
        {
            _history.HasPendingAsync().Returns(true);
            _state.GetAsync().Returns(new AdminQueueReminderState
            {
                DueAtUtc = new DateTime(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc),
                LastSentAtUtc = new DateTime(2026, 7, 28, 11, 0, 0, DateTimeKind.Utc)
            });

            await CreateSut(delayMinutes: 60).OnPendingLeftAsync(wasApproved: true);

            await _state.Received(1).UpsertAsync(
                new DateTime(2026, 7, 28, 11, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 28, 11, 0, 0, DateTimeKind.Utc),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task OnPendingLeft_RejectAfterReminder_DoesNotResetDue()
        {
            _history.HasPendingAsync().Returns(true);
            _state.GetAsync().Returns(new AdminQueueReminderState
            {
                DueAtUtc = new DateTime(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc),
                LastSentAtUtc = new DateTime(2026, 7, 28, 11, 0, 0, DateTimeKind.Utc)
            });

            await CreateSut().OnPendingLeftAsync(wasApproved: false);

            await _state.DidNotReceiveWithAnyArgs().UpsertAsync(default, default, default);
        }

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;
            public FixedTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;
            public override DateTimeOffset GetUtcNow() => _utcNow;
        }
    }
}
