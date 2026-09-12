using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;

namespace FindIFBot.IntegrationTests.Repositories
{
    public class AdminQueueReminderStateRepositoryTests
    {
        [Fact]
        public async Task Upsert_ThenGet_ReturnsSingletonState()
        {
            using var db = new SqliteTestDatabase();
            var due = new DateTime(2026, 7, 28, 11, 0, 0, DateTimeKind.Utc);
            var sent = new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc);

            await new AdminQueueReminderStateRepository(db.CreateContext())
                .UpsertAsync(due, sent);

            var loaded = await new AdminQueueReminderStateRepository(db.CreateContext()).GetAsync();

            loaded.Should().NotBeNull();
            loaded!.Id.Should().Be(AdminQueueReminderState.SingletonId);
            loaded.DueAtUtc.Should().Be(due);
            loaded.LastSentAtUtc.Should().Be(sent);
        }

        [Fact]
        public async Task Clear_NullsDueAndLastSent()
        {
            using var db = new SqliteTestDatabase();
            var repoWrite = new AdminQueueReminderStateRepository(db.CreateContext());
            await repoWrite.UpsertAsync(
                new DateTime(2026, 7, 28, 11, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc));

            await new AdminQueueReminderStateRepository(db.CreateContext()).ClearAsync();

            var loaded = await new AdminQueueReminderStateRepository(db.CreateContext()).GetAsync();
            loaded.Should().NotBeNull();
            loaded!.DueAtUtc.Should().BeNull();
            loaded.LastSentAtUtc.Should().BeNull();
        }
    }
}
