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
        private readonly UserModerationNotifier _sut;

        public UserModerationNotifierTests()
        {
            var options = Options.Create(new TelegramOptions { DirectChatLink = DirectLink });
            _sut = new UserModerationNotifier(_bot, _history, options);
        }

        [Fact]
        public async Task NotifySubmittedAsync_IncludesRequestIdInUkrainian()
        {
            await _sut.NotifySubmittedAsync(UserId, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ChatId.Identifier.Should().Be(UserId);
            sent.ParseMode.Should().Be(ParseMode.Html);
            sent.Text.Should().Contain("Запит відправлено на модерацію");
            sent.Text.Should().Contain($"🆔 <b>ID запиту:</b> #<code>{MessageId}</code>");
        }

        [Fact]
        public async Task NotifyPublishedAsync_IncludesRequestId()
        {
            const string channelLink = "https://t.me/c/1/2";

            await _sut.NotifyPublishedAsync(UserId, channelLink, MessageId);

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
            await _sut.NotifyRejectedAsync(UserId, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ReplyParameters!.MessageId.Should().Be(MessageId);
            sent.ParseMode.Should().Be(ParseMode.Html);
            sent.Text.Should().Contain($"Запит <code>#{MessageId}</code> відхилено");
        }

        [Fact]
        public async Task NotifyDuplicateAsync_IncludesRequestIdAndRepliesToOriginal()
        {
            await _sut.NotifyDuplicateAsync(UserId, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ReplyParameters!.MessageId.Should().Be(MessageId);
            sent.ParseMode.Should().Be(ParseMode.Html);
            sent.Text.Should().Contain("Схожий допис");
            sent.Text.Should().Contain($"🆔 <b>ID запиту:</b> #<code>{MessageId}</code>");
        }

        [Fact]
        public async Task NotifyAdvertisementAsync_SendsMessageToUserWithDirectChatButton()
        {
            await _sut.NotifyAdvertisementAsync(UserId, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ChatId.Identifier.Should().Be(UserId);

            var keyboard = sent.ReplyMarkup.Should().BeOfType<InlineKeyboardMarkup>().Subject;
            var button = keyboard.InlineKeyboard.SelectMany(row => row).Single();
            button.Url.Should().Be(DirectLink);
        }

        [Fact]
        public async Task NotifyAdvertisementAsync_RepliesToOriginalRequestUsingHtml()
        {
            await _sut.NotifyAdvertisementAsync(UserId, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ReplyParameters!.MessageId.Should().Be(MessageId);
            sent.ParseMode.Should().Be(ParseMode.Html);
            sent.Text.Should().Contain("реклам");
            sent.Text.Should().Contain($"🆔 <b>ID запиту:</b> #<code>{MessageId}</code>");
        }

        [Fact]
        public async Task NotifyNeedsAttentionAsync_IncludesRequestIdAndDirectChatButton()
        {
            await _sut.NotifyNeedsAttentionAsync(UserId, MessageId);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ReplyParameters!.MessageId.Should().Be(MessageId);
            sent.ParseMode.Should().Be(ParseMode.Html);
            sent.Text.Should().Contain("уточнення");
            sent.Text.Should().Contain($"🆔 <b>ID запиту:</b> #<code>{MessageId}</code>");

            var keyboard = sent.ReplyMarkup.Should().BeOfType<InlineKeyboardMarkup>().Subject;
            var button = keyboard.InlineKeyboard.SelectMany(row => row).Single();
            button.Url.Should().Be(DirectLink);
        }
    }
}
