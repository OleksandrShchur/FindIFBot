using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.Persistence;
using FindIFBot.Services.Admin;
using FindIFBot.UnitTests.TestSupport;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.UnitTests.Services.Admin
{
    public class AdminRequestNotifierTests
    {
        private const long AdminId = 1000;
        private const long UserId = 321;
        private const int MessageId = 99;
        private const int AdminInfoMessageId = 501;

        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly AdminRequestNotifier _sut;

        public AdminRequestNotifierTests()
        {
            _bot.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    var request = (SendMessageRequest)ci[0];
                    // First message is user info (no reply markup); later ones may have the keyboard.
                    var id = request.ReplyMarkup is null ? AdminInfoMessageId : AdminInfoMessageId + 1;
                    return new Message { Id = id };
                });

            _sut = new AdminRequestNotifier(_bot, Options.Create(new TelegramOptions { AdminId = AdminId }));
        }

        [Fact]
        public async Task SendToAdminAsync_TextOnly_ReturnsUserInfoMessageId()
        {
            var stored = new StoredMessage(UserId, UserId, "promo text", [], null, MessageId);
            var userInfo = new UserInfo { Id = UserId };

            var result = await _sut.SendToAdminAsync(stored, userInfo);

            result.Should().Be(AdminInfoMessageId);
            var first = _bot.SentRequests<SendMessageRequest>().First();
            first.Text.Should().Contain("Інформація про користувача");
            first.Text.Should().Contain($"🆔 <b>ID запиту:</b> #<code>{MessageId}</code>");
            first.ReplyMarkup.Should().BeNull();
        }

        [Fact]
        public async Task SendToAdminAsync_TextOnly_IncludesAdvertisementButtonWithCallbackData()
        {
            var stored = new StoredMessage(UserId, UserId, "promo text", [], null, MessageId);
            var userInfo = new UserInfo { Id = UserId };

            await _sut.SendToAdminAsync(stored, userInfo);

            var moderation = _bot.SentRequests<SendMessageRequest>()
                .Single(r => r.ReplyMarkup is InlineKeyboardMarkup);
            var buttons = ((InlineKeyboardMarkup)moderation.ReplyMarkup!)
                .InlineKeyboard.SelectMany(row => row).ToList();

            var adsButton = buttons.Should()
                .ContainSingle(b => b.CallbackData == $"!ask|{UserId}|{MessageId}").Subject;
            adsButton.Text.Should().Contain("Реклама");
        }

        [Fact]
        public async Task SendToAdminAsync_TextOnly_IncludesNeedsAttentionButtonWithCallbackData()
        {
            var stored = new StoredMessage(UserId, UserId, "promo text", [], null, MessageId);
            var userInfo = new UserInfo { Id = UserId };

            await _sut.SendToAdminAsync(stored, userInfo);

            var moderation = _bot.SentRequests<SendMessageRequest>()
                .Single(r => r.ReplyMarkup is InlineKeyboardMarkup);
            var buttons = ((InlineKeyboardMarkup)moderation.ReplyMarkup!)
                .InlineKeyboard.SelectMany(row => row).ToList();

            var attentionButton = buttons.Should()
                .ContainSingle(b => b.CallbackData == $"*ask|{UserId}|{MessageId}").Subject;
            attentionButton.Text.Should().Contain("Уточнити");
        }

        [Fact]
        public async Task SendToAdminAsync_TextOnly_SendsHtmlTextLinks()
        {
            var entities = new[]
            {
                new StoredMessageEntity("TextLink", 0, 4, Url: "https://example.com/hidden")
            };
            var stored = new StoredMessage(UserId, UserId, "post about town", [], null, MessageId, entities);
            var userInfo = new UserInfo { Id = UserId };

            await _sut.SendToAdminAsync(stored, userInfo);

            var moderation = _bot.SentRequests<SendMessageRequest>()
                .Single(r => r.ReplyMarkup is InlineKeyboardMarkup);
            moderation.ParseMode.Should().Be(Telegram.Bot.Types.Enums.ParseMode.Html);
            moderation.Text.Should().Contain("<a href=\"https://example.com/hidden\">post</a>");
            moderation.Text.Should().Contain(" about town");
        }

        [Fact]
        public async Task SendToAdminAsync_TextOnly_KeepsApproveRejectDuplicateButtons()
        {
            var stored = new StoredMessage(UserId, UserId, "promo text", [], null, MessageId);
            var userInfo = new UserInfo { Id = UserId };

            await _sut.SendToAdminAsync(stored, userInfo);

            var moderation = _bot.SentRequests<SendMessageRequest>()
                .Single(r => r.ReplyMarkup is InlineKeyboardMarkup);
            var callbackData = ((InlineKeyboardMarkup)moderation.ReplyMarkup!)
                .InlineKeyboard.SelectMany(row => row)
                .Select(b => b.CallbackData)
                .ToList();

            callbackData.Should().Contain($"+ask|{UserId}|{MessageId}");
            callbackData.Should().Contain($"-ask|{UserId}|{MessageId}");
            callbackData.Should().Contain($"?ask|{UserId}|{MessageId}");
            callbackData.Should().Contain($"!ask|{UserId}|{MessageId}");
            callbackData.Should().Contain($"*ask|{UserId}|{MessageId}");
        }
    }
}
