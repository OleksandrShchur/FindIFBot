using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers;
using FindIFBot.Helpers.Logs;
using FindIFBot.Services.Admin;
using FindIFBot.UnitTests.TestSupport;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.UnitTests.Services.Admin
{
    public class AdminQueueReminderServiceTests
    {
        private const long AdminId = 1000;
        private const int UserMessageId = 42;
        private const int AdminInfoMessageId = 777;

        private readonly IAdminQueueReminderStateRepository _state = Substitute.For<IAdminQueueReminderStateRepository>();
        private readonly IUserRequestHistoryRepository _history = Substitute.For<IUserRequestHistoryRepository>();
        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly IAppLogger<AdminQueueReminderService> _logger = Substitute.For<IAppLogger<AdminQueueReminderService>>();
        private readonly IWorkingHours _workingHours = KyivWorkingHours.CreateDefault();

        private AdminQueueReminderService CreateSut(TimeProvider time, int delayMinutes = 60) =>
            new(
                _state,
                _history,
                _bot,
                Options.Create(new TelegramOptions { AdminId = AdminId }),
                Options.Create(new ReminderOptions { DelayMinutes = delayMinutes }),
                _workingHours,
                time,
                _logger);

        [Fact]
        public async Task ProcessDue_WhenNotDue_DoesNothing()
        {
            var now = FixedKyivTime(12, 0);
            _state.GetAsync().Returns(new AdminQueueReminderState
            {
                DueAtUtc = now.GetUtcNow().UtcDateTime.AddMinutes(30)
            });

            await CreateSut(now).ProcessDueAsync();

            await _history.DidNotReceiveWithAnyArgs().GetOldestPendingAsync(default);
            _bot.SentRequests<SendMessageRequest>().Should().BeEmpty();
        }

        [Fact]
        public async Task ProcessDue_WhenDueButOutsideWorkingHours_DoesNotSend()
        {
            var now = FixedKyivTime(23, 0);
            _state.GetAsync().Returns(new AdminQueueReminderState
            {
                DueAtUtc = now.GetUtcNow().UtcDateTime.AddMinutes(-5)
            });
            _history.GetOldestPendingAsync().Returns(new UserRequest
            {
                Id = Guid.NewGuid(),
                UserId = 1,
                Status = RequestStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                UserMessageId = UserMessageId,
                AdminInfoMessageId = AdminInfoMessageId
            });

            await CreateSut(now).ProcessDueAsync();

            _bot.SentRequests<SendMessageRequest>().Should().BeEmpty();
            await _state.DidNotReceiveWithAnyArgs().UpsertAsync(default, default, default);
        }

        [Fact]
        public async Task ProcessDue_WhenDueDuringWorkingHours_SendsReplyToOldestAndReschedules()
        {
            var now = FixedKyivTime(12, 0);
            var due = now.GetUtcNow().UtcDateTime.AddMinutes(-1);
            _state.GetAsync().Returns(new AdminQueueReminderState { DueAtUtc = due });
            _history.GetOldestPendingAsync().Returns(new UserRequest
            {
                Id = Guid.NewGuid(),
                UserId = 9,
                Status = RequestStatus.Pending,
                SubmittedAt = DateTime.UtcNow.AddHours(-2),
                UserMessageId = UserMessageId,
                AdminInfoMessageId = AdminInfoMessageId
            });
            _bot.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(new Message { Id = 900 });

            await CreateSut(now, delayMinutes: 60).ProcessDueAsync();

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ChatId.Identifier.Should().Be(AdminId);
            sent.Text.Should().Contain($"#<code>{UserMessageId}</code>");
            sent.Text.Should().Contain("Нагадування");
            sent.ReplyParameters!.MessageId.Should().Be(AdminInfoMessageId);
            sent.ParseMode.Should().Be(ParseMode.Html);

            await _state.Received(1).UpsertAsync(
                now.GetUtcNow().UtcDateTime.AddHours(1),
                now.GetUtcNow().UtcDateTime,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessDue_WhenDueButQueueEmpty_ClearsState()
        {
            var now = FixedKyivTime(12, 0);
            _state.GetAsync().Returns(new AdminQueueReminderState
            {
                DueAtUtc = now.GetUtcNow().UtcDateTime.AddMinutes(-1)
            });
            _history.GetOldestPendingAsync().Returns((UserRequest?)null);

            await CreateSut(now).ProcessDueAsync();

            await _state.Received(1).ClearAsync(Arg.Any<CancellationToken>());
            _bot.SentRequests<SendMessageRequest>().Should().BeEmpty();
        }

        [Fact]
        public async Task ProcessDue_WithoutAdminInfoMessageId_SendsWithoutReply()
        {
            var now = FixedKyivTime(12, 0);
            _state.GetAsync().Returns(new AdminQueueReminderState
            {
                DueAtUtc = now.GetUtcNow().UtcDateTime.AddMinutes(-1)
            });
            _history.GetOldestPendingAsync().Returns(new UserRequest
            {
                Id = Guid.NewGuid(),
                UserId = 1,
                Status = RequestStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                UserMessageId = UserMessageId,
                AdminInfoMessageId = null
            });
            _bot.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(new Message { Id = 901 });

            await CreateSut(now).ProcessDueAsync();

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ReplyParameters.Should().BeNull();
            sent.Text.Should().Contain("Посилання на тред модерації відсутнє");
        }

        private static TimeProvider FixedKyivTime(int hour, int minute)
        {
            var kyiv = ResolveKyivTimeZone();
            var local = new DateTime(2026, 7, 28, hour, minute, 0, DateTimeKind.Unspecified);
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
