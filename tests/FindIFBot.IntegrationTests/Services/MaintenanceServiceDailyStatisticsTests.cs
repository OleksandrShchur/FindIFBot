using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.Helpers;
using FindIFBot.IntegrationTests.Repositories;
using FindIFBot.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace FindIFBot.IntegrationTests.Services
{
    public class MaintenanceServiceDailyStatisticsTests
    {
        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly IWebHostEnvironment _env = Substitute.For<IWebHostEnvironment>();

        public MaintenanceServiceDailyStatisticsTests()
        {
            _bot.SendRequest(Arg.Any<GetChatMemberCountRequest>(), Arg.Any<CancellationToken>())
                .Returns(5_678);
            _bot.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(new Message { Id = 1 });
        }

        [Fact]
        public async Task SendDailyStatisticsAsync_PersistsSnapshotAndSendsTable()
        {
            using var db = new SqliteTestDatabase();
            var kyivNow = FixedKyivTime(2026, 8, 4, 21, 0);
            var (dayStart, dayEnd) = KyivWorkingHours.GetKyivDayUtcRange(new DateOnly(2026, 8, 4));

            await using (var seed = db.CreateContext())
            {
                seed.UserSessions.Add(new UserSession { UserId = 1, CreatedUtc = DateTime.UtcNow });
                seed.UserSessions.Add(new UserSession { UserId = 2, CreatedUtc = DateTime.UtcNow });
                seed.UserRequests.Add(RequestBuilder.Create(
                    userId: 10,
                    userMessageId: 1,
                    status: RequestStatus.Approved,
                    channelLink: "https://t.me/c/1",
                    publishedAtUtc: dayStart.AddHours(2)));
                seed.UserRequests.Add(RequestBuilder.Create(
                    userId: 11,
                    userMessageId: 2,
                    status: RequestStatus.Approved,
                    channelLink: "https://t.me/c/2",
                    publishedAtUtc: dayStart.AddHours(5)));
                seed.UserRequests.Add(RequestBuilder.Create(
                    userId: 12,
                    userMessageId: 3,
                    status: RequestStatus.Approved,
                    channelLink: "https://t.me/c/3",
                    publishedAtUtc: dayEnd)); // next Kyiv day — excluded
                seed.UserRequests.Add(RequestBuilder.Create(
                    userId: 13,
                    userMessageId: 4,
                    status: RequestStatus.Approved,
                    channelLink: "https://t.me/c/4")); // legacy null PublishedAtUtc — excluded
                await seed.SaveChangesAsync();
            }

            var sut = CreateSut(db, kyivNow);
            await sut.SendDailyStatisticsAsync();

            await using (var assertDb = db.CreateContext())
            {
                var row = await assertDb.ChannelDailyStatistics.SingleAsync();
                row.Date.Should().Be(new DateOnly(2026, 8, 4));
                row.BotUserCount.Should().Be(2);
                row.ChannelSubscriberCount.Should().Be(5_678);
                row.PostsCount.Should().Be(2);
            }

            var sent = _bot.ReceivedCalls()
                .Where(c => c.GetMethodInfo().Name == nameof(ITelegramBotClient.SendRequest))
                .Select(c => c.GetArguments()[0])
                .OfType<SendMessageRequest>()
                .Should().ContainSingle().Subject;

            sent.Text.Should().Contain("Daily Statistics — 04.08.2026");
            sent.Text.Should().Contain("<pre>");
            sent.Text.Should().Contain("Bot users");
            sent.Text.Should().Contain("Channel subscribers");
            sent.Text.Should().Contain("Posts today");
            sent.Text.Should().Contain("5,678");
        }

        [Fact]
        public async Task SendDailyStatisticsAsync_WhenCalledTwice_UpsertsSameDayRow()
        {
            using var db = new SqliteTestDatabase();
            var kyivNow = FixedKyivTime(2026, 8, 4, 22, 30);

            await using (var seed = db.CreateContext())
            {
                seed.UserSessions.Add(new UserSession { UserId = 1, CreatedUtc = DateTime.UtcNow });
                await seed.SaveChangesAsync();
            }

            var sut = CreateSut(db, kyivNow);
            await sut.SendDailyStatisticsAsync();

            await using (var mid = db.CreateContext())
            {
                mid.UserSessions.Add(new UserSession { UserId = 2, CreatedUtc = DateTime.UtcNow });
                await mid.SaveChangesAsync();
            }

            _bot.SendRequest(Arg.Any<GetChatMemberCountRequest>(), Arg.Any<CancellationToken>())
                .Returns(6_000);

            await sut.SendDailyStatisticsAsync();

            await using (var assertDb = db.CreateContext())
            {
                var rows = await assertDb.ChannelDailyStatistics.ToListAsync();
                rows.Should().HaveCount(1);
                rows[0].BotUserCount.Should().Be(2);
                rows[0].ChannelSubscriberCount.Should().Be(6_000);
            }
        }

        private MaintenanceService CreateSut(SqliteTestDatabase db, TimeProvider timeProvider) =>
            new(
                Substitute.For<ILogger<MaintenanceService>>(),
                _env,
                _bot,
                Options.Create(new TelegramOptions
                {
                    UserOutputChannel = "@ask_frankivsk",
                    LogsOutputChannel = "-1001",
                    LogsThreadId = 7
                }),
                db.CreateContext(),
                timeProvider);

        private static TimeProvider FixedKyivTime(int year, int month, int day, int hour, int minute)
        {
            var kyiv = ResolveKyivTimeZone();
            var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
            var utc = TimeZoneInfo.ConvertTimeToUtc(local, kyiv);
            return new FixedTimeProvider(new DateTimeOffset(utc, TimeSpan.Zero));
        }

        private static TimeZoneInfo ResolveKyivTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
            }
        }

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }
    }
}
