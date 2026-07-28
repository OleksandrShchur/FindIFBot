using FindIFBot.Configuration;
using FindIFBot.EF.Repositories;
using FindIFBot.Services.Admin;
using FindIFBot.UnitTests.TestSupport;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.UnitTests.Services.Admin
{
    public class UserModerationNotifierTests
    {
        private const long UserId = 555;
        private const int MessageId = 77;
        private const string DirectLink = "https://t.me/ask_frankivsk?direct";

        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly IUserRequestHistoryRepository _history = Substitute.For<IUserRequestHistoryRepository>();

        private UserModerationNotifier CreateSut(TimeProvider timeProvider)
        {
            var options = Options.Create(new TelegramOptions { DirectChatLink = DirectLink });
            return new UserModerationNotifier(_bot, _history, options, timeProvider);
        }

        [Fact]
        public async Task NotifySubmittedAsync_DuringWorkingHours_UsesAsapMessage()
        {
            var sut = CreateSut(FixedKyivTime(12, 0));

            await sut.NotifySubmittedAsync(UserId, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ChatId.Identifier.Should().Be(UserId);
            sent.ParseMode.Should().Be(ParseMode.Html);
            sent.Text.Should().Contain("Запит відправлено на модерацію");
            sent.Text.Should().Contain("наші модератори скоро перевірять ваш допис");
            sent.Text.Should().NotContain("робочі години");
            sent.Text.Should().Contain($"🆔 <b>ID запиту:</b> #<code>{MessageId}</code>");
        }

        [Fact]
        public async Task NotifySubmittedAsync_OutsideWorkingHours_UsesWorkingHoursMessage()
        {
            var sut = CreateSut(FixedKyivTime(23, 0));

            await sut.NotifySubmittedAsync(UserId, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ChatId.Identifier.Should().Be(UserId);
            sent.ParseMode.Should().Be(ParseMode.Html);
            sent.Text.Should().Contain("Запит відправлено на модерацію");
            sent.Text.Should().Contain("робочі години — з 9:00 до 22:00");
            sent.Text.Should().Contain("київським часом");
            sent.Text.Should().NotContain("наші модератори скоро перевірять ваш допис");
            sent.Text.Should().Contain($"🆔 <b>ID запиту:</b> #<code>{MessageId}</code>");
        }

        [Fact]
        public async Task NotifyPublishedAsync_IncludesRequestId()
        {
            const string channelLink = "https://t.me/c/1/2";
            var sut = CreateSut(TimeProvider.System);

            await sut.NotifyPublishedAsync(UserId, channelLink, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ChatId.Identifier.Should().Be(UserId);
            sent.ParseMode.Should().Be(ParseMode.Html);
            sent.Text.Should().Contain("Готово");
            sent.Text.Should().Contain(channelLink);
            sent.Text.Should().Contain($"Ваш запит <code>#{MessageId}</code> опубліковано");
        }

        [Fact]
        public async Task NotifyRejectedAsync_IncludesRequestIdAndRepliesToOriginal()
        {
            var sut = CreateSut(TimeProvider.System);

            await sut.NotifyRejectedAsync(UserId, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ReplyParameters!.MessageId.Should().Be(MessageId);
            sent.ParseMode.Should().Be(ParseMode.Html);
            sent.Text.Should().Contain($"Запит <code>#{MessageId}</code> відхилено");
        }

        [Fact]
        public async Task NotifyDuplicateAsync_IncludesRequestIdAndRepliesToOriginal()
        {
            var sut = CreateSut(TimeProvider.System);

            await sut.NotifyDuplicateAsync(UserId, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ReplyParameters!.MessageId.Should().Be(MessageId);
            sent.ParseMode.Should().Be(ParseMode.Html);
            sent.Text.Should().Contain("Схожий допис");
            sent.Text.Should().Contain($"🆔 <b>ID запиту:</b> #<code>{MessageId}</code>");
        }

        [Fact]
        public async Task NotifyAdvertisementAsync_SendsMessageToUserWithDirectChatButton()
        {
            var sut = CreateSut(TimeProvider.System);

            await sut.NotifyAdvertisementAsync(UserId, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ChatId.Identifier.Should().Be(UserId);

            var keyboard = sent.ReplyMarkup.Should().BeOfType<InlineKeyboardMarkup>().Subject;
            var button = keyboard.InlineKeyboard.SelectMany(row => row).Single();
            button.Url.Should().Be(DirectLink);
        }

        [Fact]
        public async Task NotifyAdvertisementAsync_RepliesToOriginalRequestUsingHtml()
        {
            var sut = CreateSut(TimeProvider.System);

            await sut.NotifyAdvertisementAsync(UserId, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ReplyParameters!.MessageId.Should().Be(MessageId);
            sent.ParseMode.Should().Be(ParseMode.Html);
            sent.Text.Should().Contain("реклам");
            sent.Text.Should().Contain($"🆔 <b>ID запиту:</b> #<code>{MessageId}</code>");
        }

        [Fact]
        public async Task NotifyNeedsAttentionAsync_IncludesRequestIdAndDirectChatButton()
        {
            var sut = CreateSut(TimeProvider.System);

            await sut.NotifyNeedsAttentionAsync(UserId, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ReplyParameters!.MessageId.Should().Be(MessageId);
            sent.ParseMode.Should().Be(ParseMode.Html);
            sent.Text.Should().Contain("уточнення");
            sent.Text.Should().Contain($"🆔 <b>ID запиту:</b> #<code>{MessageId}</code>");

            var keyboard = sent.ReplyMarkup.Should().BeOfType<InlineKeyboardMarkup>().Subject;
            var button = keyboard.InlineKeyboard.SelectMany(row => row).Single();
            button.Url.Should().Be(DirectLink);
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
