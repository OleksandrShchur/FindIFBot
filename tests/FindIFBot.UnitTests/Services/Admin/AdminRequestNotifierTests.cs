using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Repositories;
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
        private readonly IUserRequestHistoryRepository _history = Substitute.For<IUserRequestHistoryRepository>();
        private readonly AdminRequestNotifier _sut;

        public AdminRequestNotifierTests()
        {
            var nextId = AdminInfoMessageId;
            _history.GetStatusCountsByUserIdAsync(Arg.Any<long>())
                .Returns(new Dictionary<RequestStatus, int>());
            _bot.SendRequest(Arg.Any<SendRichMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(_ => new Message { Id = nextId++ });
            _bot.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(_ => new Message { Id = nextId++ });
            _bot.SendRequest(Arg.Any<SendMediaGroupRequest>(), Arg.Any<CancellationToken>())
                .Returns(_ => new[] { new Message { Id = nextId++ } });

            _sut = new AdminRequestNotifier(
                _bot,
                Options.Create(new TelegramOptions { AdminId = AdminId }),
                _history);
        }

        [Fact]
        public async Task SendToAdminAsync_TextOnly_ReturnsUserInfoMessageId()
        {
            var stored = new StoredMessage(UserId, UserId, "promo text", [], null, MessageId);
            var userInfo = new UserInfo
            {
                Id = UserId,
                UserName = "demo_user",
                FirstName = "Ada",
                LastName = "Lovelace",
                LanguageCode = "en",
                IsBot = false,
                IsPremium = true
            };

            var result = await _sut.SendToAdminAsync(stored, userInfo);

            result.Should().Be(AdminInfoMessageId);
            var first = _bot.SentRequests<SendRichMessageRequest>().Should().ContainSingle().Subject;
            var html = first.RichMessage.Html!;
            html.Should().Contain("Інформація про користувача");
            html.Should().Contain($"🆔 <b>ID запиту:</b> #<code>{MessageId}</code>");
            html.Should().Contain($"<tr><td>ID</td><td>{UserId}</td></tr>");
            html.Should().Contain("<tr><td>UserName</td><td>@demo_user</td></tr>");
            html.Should().Contain("<tr><td>First Name</td><td>Ada</td></tr>");
            html.Should().Contain("<tr><td>Last Name</td><td>Lovelace</td></tr>");
            html.Should().Contain("<tr><td>Language Code</td><td>en</td></tr>");
            html.Should().Contain("<tr><td>Is Bot</td><td>❌ Ні</td></tr>");
            html.Should().Contain("<tr><td>Is Premium</td><td>✅ Так</td></tr>");
            html.Should().Contain("Інформація про користувача:\n<table><tr><td>ID</td><td>");
            first.ReplyMarkup.Should().BeNull();
        }

        [Fact]
        public async Task SendToAdminAsync_TextOnly_SendsRichInfoThenTwoMessages_ContentWithoutKeyboard()
        {
            var stored = new StoredMessage(UserId, UserId, "promo text", [], null, MessageId);
            var userInfo = new UserInfo { Id = UserId };

            await _sut.SendToAdminAsync(stored, userInfo);

            var info = _bot.SentRequests<SendRichMessageRequest>().Should().ContainSingle().Subject;
            info.RichMessage.Html.Should().Contain("Інформація про користувача");
            info.ReplyMarkup.Should().BeNull();

            var messages = _bot.SentRequests<SendMessageRequest>();
            messages.Should().HaveCount(2);

            messages[0].Text.Should().Be("promo text");
            messages[0].ReplyMarkup.Should().BeNull();

            messages[1].Text.Should().Be($"Дії модерації до #<code>{MessageId}</code>");
            messages[1].ReplyMarkup.Should().BeOfType<InlineKeyboardMarkup>();
            messages[1].ParseMode.Should().Be(Telegram.Bot.Types.Enums.ParseMode.Html);
        }

        [Fact]
        public async Task SendToAdminAsync_IncludesRequestStatsTableWithCurrentPending()
        {
            _history.GetStatusCountsByUserIdAsync(UserId)
                .Returns(new Dictionary<RequestStatus, int>
                {
                    [RequestStatus.Approved] = 2,
                    [RequestStatus.Rejected] = 1
                });

            var stored = new StoredMessage(UserId, UserId, "promo text", [], null, MessageId);
            var userInfo = new UserInfo { Id = UserId };

            await _sut.SendToAdminAsync(stored, userInfo);

            var html = _bot.SentRequests<SendRichMessageRequest>().Single().RichMessage.Html!;
            html.Should().Contain("<b>User Requests Statistic</b>");
            html.Should().Contain("<th>Status</th><th>Count</th>");
            html.Should().Contain("<tr><td>Pending</td><td>1</td></tr>");
            html.Should().Contain("<tr><td>Approved</td><td>2</td></tr>");
            html.Should().Contain("<tr><td>Rejected</td><td>1</td></tr>");
            html.Should().Contain("<tr><td><b>Total</b></td><td><b>4</b></td></tr>");
            html.Should().NotContain("Duplicate");
            html.Should().NotContain("Advertisement");
            html.Should().NotContain("NeedsAttention");
        }

        [Fact]
        public async Task SendToAdminAsync_TextOnly_IncludesAdvertisementButtonWithCallbackData()
        {
            var stored = new StoredMessage(UserId, UserId, "promo text", [], null, MessageId);
            var userInfo = new UserInfo { Id = UserId };

            await _sut.SendToAdminAsync(stored, userInfo);

            var moderation = ActionsMessage();
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

            var moderation = ActionsMessage();
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

            var content = _bot.SentRequests<SendMessageRequest>()
                .Single(r => r.ReplyMarkup is null);
            content.ParseMode.Should().Be(Telegram.Bot.Types.Enums.ParseMode.Html);
            content.Text.Should().Contain("<a href=\"https://example.com/hidden\">post</a>");
            content.Text.Should().Contain(" about town");
        }

        [Fact]
        public async Task SendToAdminAsync_TextOnly_KeepsApproveRejectDuplicateButtons()
        {
            var stored = new StoredMessage(UserId, UserId, "promo text", [], null, MessageId);
            var userInfo = new UserInfo { Id = UserId };

            await _sut.SendToAdminAsync(stored, userInfo);

            var moderation = ActionsMessage();
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

        [Fact]
        public async Task SendToAdminAsync_WithPhotos_SendsMediaGroupThenActionsMessage()
        {
            var stored = new StoredMessage(UserId, UserId, "caption", ["photo-1"], null, MessageId);
            var userInfo = new UserInfo { Id = UserId };

            await _sut.SendToAdminAsync(stored, userInfo);

            _bot.SentRequests<SendMediaGroupRequest>().Should().ContainSingle();

            _bot.SentRequests<SendRichMessageRequest>().Should().ContainSingle();
            var info = _bot.SentRequests<SendRichMessageRequest>().Single();
            info.RichMessage.Html.Should().Contain("Інформація про користувача");
            info.ReplyMarkup.Should().BeNull();

            var messages = _bot.SentRequests<SendMessageRequest>();
            messages.Should().HaveCount(1);
            messages[0].Text.Should().Be($"Дії модерації до #<code>{MessageId}</code>");
            messages[0].ReplyMarkup.Should().BeOfType<InlineKeyboardMarkup>();
        }

        private SendMessageRequest ActionsMessage() =>
            _bot.SentRequests<SendMessageRequest>()
                .Single(r => r.ReplyMarkup is InlineKeyboardMarkup);
    }
}
