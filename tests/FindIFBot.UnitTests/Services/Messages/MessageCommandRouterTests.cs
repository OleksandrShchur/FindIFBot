using FindIFBot.Configuration;
using FindIFBot.EF.Repositories;
using FindIFBot.Handlers;
using FindIFBot.Helpers.Logs;
using FindIFBot.Services.Messages;
using FindIFBot.UnitTests.TestSupport;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.UnitTests.Services.Messages
{
    public class MessageCommandRouterTests
    {
        private const long UserId = 100;
        private const long AdminId = 999;
        private const long ChatId = 100;
        private const string DirectLink = "https://t.me/ask_frankivsk?direct";

        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly IUserRequestHistoryRepository _history = Substitute.For<IUserRequestHistoryRepository>();
        private readonly IAppLogger<MessageCommandRouter> _logger = Substitute.For<IAppLogger<MessageCommandRouter>>();
        private readonly MessageCommandRouter _sut;

        public MessageCommandRouterTests()
        {
            var telegramOptions = Options.Create(new TelegramOptions
            {
                DirectChatLink = DirectLink,
                AdminId = AdminId
            });
            var historyHandler = new HistoryHandler(
                _history,
                Options.Create(new HistoryOptions()),
                telegramOptions);
            var adsHandler = new AdsCollaborationHandler(telegramOptions);
            var pendingHandler = new AdminPendingHandler(
                _history,
                Options.Create(new HistoryOptions()),
                telegramOptions);
            var helpHandler = new HelpHandler();

            _sut = new MessageCommandRouter(
                _bot,
                new IAsyncCommandHandler[] { historyHandler, adsHandler, pendingHandler },
                _history,
                telegramOptions,
                _logger,
                helpHandler,
                new PolicyHandler(),
                new SupportUsHandler(telegramOptions),
                new ChannelLinkHandler(telegramOptions),
                new UnknownHandler(helpHandler));
        }

        [Fact]
        public async Task Given_AdsCollaborationCaption_When_Route_Then_SendsPolicyWithDirectRedirectButton()
        {
            var message = TelegramBuilder.TextMessage("🤝 Реклама та співпраця", userId: UserId, chatId: ChatId);

            await _sut.RouteAsync(message, "🤝 реклама та співпраця");

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.Text.Should().Contain("Реклама та співпраця");
            var keyboard = sent.ReplyMarkup.Should().BeOfType<InlineKeyboardMarkup>().Subject;
            keyboard.InlineKeyboard.SelectMany(row => row).Single().Url.Should().Be(DirectLink);
        }

        [Fact]
        public async Task Given_HelpCaption_When_Route_Then_SendsHelpWithReplyKeyboard()
        {
            var message = TelegramBuilder.TextMessage("ℹ️ Довідка", userId: UserId, chatId: ChatId);

            await _sut.RouteAsync(message, "ℹ️ довідка");

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.Text.Should().Contain("Що я вмію");
            sent.ReplyMarkup.Should().BeOfType<ReplyKeyboardMarkup>();
        }

        [Fact]
        public async Task Given_PendingCaptionFromAdmin_When_Route_Then_InvokesPendingHandler()
        {
            _history.GetPendingAsync(Arg.Any<int>()).Returns([]);
            _history.HasHistory(AdminId).Returns(false);
            var message = TelegramBuilder.TextMessage("⏳ Черга модерації", userId: AdminId, chatId: AdminId);

            await _sut.RouteAsync(message, "⏳ черга модерації");

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.Text.Should().Contain("Черга модерації порожня");
        }
    }
}
