using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Handlers;
using FindIFBot.Helpers.Logs;
using FindIFBot.Services.Ask;
using FindIFBot.UnitTests.TestSupport;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.UnitTests.Services.Ask
{
    public class AskFlowServiceTests
    {
        private const long UserId = 555;
        private const long ChatId = 777;

        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly IUserSessionRepository _sessions = Substitute.For<IUserSessionRepository>();
        private readonly ISubscriptionService _subscription = Substitute.For<ISubscriptionService>();
        private readonly IAppLogger<AskFlowService> _logger = Substitute.For<IAppLogger<AskFlowService>>();
        private readonly AskFlowService _sut;

        public AskFlowServiceTests()
        {
            var telegramOptions = Options.Create(new TelegramOptions { LinkToChannel = "https://t.me/findif" });
            var askHandler = new AskHandler(Options.Create(new SubmissionOptions()));

            _sut = new AskFlowService(_bot, _sessions, _subscription, _logger, telegramOptions, askHandler);
        }

        [Fact]
        public async Task Given_Callback_When_HandleCallbackAsync_Then_AnswersLoadsSessionAndStartsFlow()
        {
            var callback = TelegramBuilder.CallbackQuery("/ask", userId: UserId, chatId: ChatId);
            var session = new UserSession { UserId = UserId };
            _sessions.GetAsync(UserId).Returns(session);
            _subscription.IsSubscribedToOutputChannelAsync(UserId).Returns(true);

            await _sut.HandleCallbackAsync(callback);

            _bot.SentRequests<AnswerCallbackQueryRequest>().Should().ContainSingle()
                .Which.CallbackQueryId.Should().Be(callback.Id);
            await _sessions.Received(1).GetAsync(UserId);
            session.State.Should().Be(UserState.WaitingForAskQuery);
        }

        [Fact]
        public async Task Given_UnsubscribedUser_When_StartAsync_Then_IdleSavedAndSubscriptionPromptSent()
        {
            var session = new UserSession { UserId = UserId, State = UserState.WaitingForAskQuery };
            _subscription.IsSubscribedToOutputChannelAsync(UserId).Returns(false);

            await _sut.StartAsync(ChatId, UserId, session);

            session.State.Should().Be(UserState.Idle);
            await _sessions.Received(1).SaveAsync(session);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ChatId.Identifier.Should().Be(ChatId);
            sent.ReplyMarkup.Should().BeOfType<InlineKeyboardMarkup>();
            await _logger.Received().LogInfo("AskFlow", Arg.Is<string>(m => m.Contains("blocked")));
        }

        [Fact]
        public async Task Given_SubscribedUser_When_StartAsync_Then_WaitingStateSavedAndAskPromptSent()
        {
            var session = new UserSession { UserId = UserId, State = UserState.Idle };
            _subscription.IsSubscribedToOutputChannelAsync(UserId).Returns(true);

            await _sut.StartAsync(ChatId, UserId, session);

            session.State.Should().Be(UserState.WaitingForAskQuery);
            await _sessions.Received(1).SaveAsync(session);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ChatId.Identifier.Should().Be(ChatId);
            sent.ReplyMarkup.Should().BeOfType<ReplyKeyboardRemove>();
            await _logger.Received().LogInfo("AskFlow", Arg.Is<string>(m => m.Contains("started ask flow")));
        }
    }
}
