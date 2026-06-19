using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;

namespace FindIFBot.IntegrationTests.Repositories
{
    public class UserSessionRepositoryTests
    {
        [Fact]
        public async Task Given_NoSession_When_GetAsync_Then_ReturnsNewIdleSession()
        {
            using var db = new SqliteTestDatabase();
            var repo = new UserSessionRepository(db.CreateContext());

            var session = await repo.GetAsync(999);

            session.Should().NotBeNull();
            session.UserId.Should().Be(999);
            session.State.Should().Be(UserState.Idle);
        }

        [Fact]
        public async Task Given_NewSession_When_SaveAsync_Then_Inserts()
        {
            using var db = new SqliteTestDatabase();
            await new UserSessionRepository(db.CreateContext())
                .SaveAsync(new UserSession { UserId = 1, State = UserState.WaitingForAskQuery });

            var stored = await new UserSessionRepository(db.CreateContext()).GetAsync(1);
            stored.State.Should().Be(UserState.WaitingForAskQuery);
        }

        [Fact]
        public async Task Given_ExistingSession_When_SaveAsync_Then_Updates()
        {
            using var db = new SqliteTestDatabase();
            await new UserSessionRepository(db.CreateContext())
                .SaveAsync(new UserSession { UserId = 1, State = UserState.WaitingForAskQuery });

            await new UserSessionRepository(db.CreateContext())
                .SaveAsync(new UserSession { UserId = 1, State = UserState.Idle });

            var stored = await new UserSessionRepository(db.CreateContext()).GetAsync(1);
            stored.State.Should().Be(UserState.Idle);
            await using var assertContext = db.CreateContext();
            assertContext.UserSessions.Count(s => s.UserId == 1).Should().Be(1);
        }

        [Fact]
        public async Task Given_ExistingSession_When_ResetAsync_Then_Removes()
        {
            using var db = new SqliteTestDatabase();
            await new UserSessionRepository(db.CreateContext())
                .SaveAsync(new UserSession { UserId = 1, State = UserState.WaitingForAskQuery });

            await new UserSessionRepository(db.CreateContext()).ResetAsync(1);

            await using var assertContext = db.CreateContext();
            assertContext.UserSessions.Any(s => s.UserId == 1).Should().BeFalse();
        }
    }
}
