using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Handlers;
using FindIFBot.Helpers.Logs;
using FindIFBot.Persistence;
using FindIFBot.Services.Ask;
using FindIFBot.Services.Messages;
using FindIFBot.UnitTests.TestSupport;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.UnitTests.Services.Messages
{
    public class MessageDispatchServiceTests
    {
        private const long UserId = 100;

        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly IUserSessionRepository _sessions = Substitute.For<IUserSessionRepository>();
        private readonly IUserRequestHistoryRepository _history = Substitute.For<IUserRequestHistoryRepository>();
        private readonly IMediaGroupHandler _mediaGroupHandler = Substitute.For<IMediaGroupHandler>();
        private readonly IMessageStorageService _storage = Substitute.For<IMessageStorageService>();
        private readonly ISubmissionValidator _validator = Substitute.For<ISubmissionValidator>();
        private readonly IAskConfirmationService _confirmation = Substitute.For<IAskConfirmationService>();
        private readonly IAskFlowService _askFlow = Substitute.For<IAskFlowService>();
        private readonly IMessageCommandRouter _commandRouter = Substitute.For<IMessageCommandRouter>();
        private readonly IAppLogger<MessageDispatchService> _logger = Substitute.For<IAppLogger<MessageDispatchService>>();
        private readonly StartHandler _startHandler;
        private readonly MessageDispatchService _sut;

        public MessageDispatchServiceTests()
        {
            _startHandler = new StartHandler(_history);
            _storage.StoreSingleAsync(Arg.Any<Message>(), Arg.Any<string?>(), Arg.Any<IReadOnlyList<string>>())
                .Returns(ci => new StoredMessage(
                    ChatId: ((Message)ci[0]).Chat.Id,
                    UserId: UserId,
                    Text: (string?)ci[1],
                    Photos: (IReadOnlyList<string>)ci[2],
                    MediaGroupId: null,
                    MessageId: ((Message)ci[0]).MessageId));

            _sut = new MessageDispatchService(
                _bot,
                _sessions,
                _history,
                new IAsyncCommandHandler[] { _startHandler },
                _mediaGroupHandler,
                _storage,
                _validator,
                _confirmation,
                _askFlow,
                _commandRouter,
                _logger);
        }

        private void GivenSession(UserState state) =>
            _sessions.GetAsync(UserId).Returns(new UserSession { UserId = UserId, State = state });

        [Fact]
        public async Task Given_MediaGroupMessage_When_Handle_Then_BuffersAndStopsProcessing()
        {
            var message = TelegramBuilder.TextMessage("caption", userId: UserId, mediaGroupId: "mg-1");

            await _sut.HandleAsync(message);

            await _mediaGroupHandler.Received(1).BufferAsync(message, UserId);
            await _sessions.DidNotReceive().GetAsync(Arg.Any<long>());
            await _storage.DidNotReceive().StoreSingleAsync(Arg.Any<Message>(), Arg.Any<string?>(), Arg.Any<IReadOnlyList<string>>());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Given_WaitingForAskQueryWithInvalidPayload_When_Handle_Then_SendsErrorWithHistoryAwareKeyboardAndResets(bool hasHistory)
        {
            GivenSession(UserState.WaitingForAskQuery);
            _history.HasHistory(UserId).Returns(hasHistory);
            _validator.ValidateSingleMessage(Arg.Any<Message>(), Arg.Any<string?>(), Arg.Any<int>())
                .Returns(SubmissionValidationResult.Invalid("validation error"));
            var message = TelegramBuilder.TextMessage("bad", userId: UserId);

            await _sut.HandleAsync(message);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.Text.Should().Be("validation error");
            var keyboard = sent.ReplyMarkup.Should().BeOfType<ReplyKeyboardMarkup>().Subject;
            HasHistoryButton(keyboard).Should().Be(hasHistory);

            await _storage.DidNotReceive().StoreSingleAsync(Arg.Any<Message>(), Arg.Any<string?>(), Arg.Any<IReadOnlyList<string>>());
            await _sessions.Received().SaveAsync(Arg.Is<UserSession>(s => s.State == UserState.Idle));
        }

        [Fact]
        public async Task Given_StartCommand_When_Handle_Then_ResetsSessionAndInvokesStartHandler()
        {
            GivenSession(UserState.Idle);
            var message = TelegramBuilder.TextMessage("/start", userId: UserId);

            await _sut.HandleAsync(message);

            await _sessions.Received().SaveAsync(Arg.Is<UserSession>(s => s.State == UserState.Idle));
            // StartHandler greeting is the behavioral signal that it executed.
            _bot.SentRequests<SendMessageRequest>().Should().Contain(r => r.Text!.Contains("Привіт"));
            await _confirmation.DidNotReceiveWithAnyArgs().SendConfirmationAsync(default!, default!);
            await _askFlow.DidNotReceiveWithAnyArgs().StartAsync(default, default, default!);
        }

        [Fact]
        public async Task Given_WaitingForAskQueryWithValidPayload_When_Handle_Then_InvokesConfirmation()
        {
            GivenSession(UserState.WaitingForAskQuery);
            _validator.ValidateSingleMessage(Arg.Any<Message>(), Arg.Any<string?>(), Arg.Any<int>())
                .Returns(SubmissionValidationResult.Valid());
            var message = TelegramBuilder.TextMessage("my real request", userId: UserId);

            await _sut.HandleAsync(message);

            await _confirmation.Received(1).SendConfirmationAsync(message, Arg.Is<UserSession>(s => s.UserId == UserId));
            await _askFlow.DidNotReceiveWithAnyArgs().StartAsync(default, default, default!);
            await _commandRouter.DidNotReceiveWithAnyArgs().RouteAsync(default!, default!);
        }

        [Fact]
        public async Task Given_AskCommand_When_Handle_Then_InvokesAskFlowStart()
        {
            GivenSession(UserState.Idle);
            var message = TelegramBuilder.TextMessage("/ask", userId: UserId, chatId: 200);

            await _sut.HandleAsync(message);

            await _askFlow.Received(1).StartAsync(200, UserId, Arg.Is<UserSession>(s => s.UserId == UserId));
            await _storage.Received(1).StoreSingleAsync(message, Arg.Any<string?>(), Arg.Any<IReadOnlyList<string>>());
        }

        [Fact]
        public async Task Given_OtherText_When_Handle_Then_RoutedThroughCommandRouter()
        {
            GivenSession(UserState.Idle);
            var message = TelegramBuilder.TextMessage("Random Text", userId: UserId);

            await _sut.HandleAsync(message);

            await _commandRouter.Received(1).RouteAsync(message, "random text");
            await _askFlow.DidNotReceiveWithAnyArgs().StartAsync(default, default, default!);
            await _confirmation.DidNotReceiveWithAnyArgs().SendConfirmationAsync(default!, default!);
        }

        private static bool HasHistoryButton(ReplyKeyboardMarkup keyboard) =>
            keyboard.Keyboard.SelectMany(row => row).Any(b => b.Text.Contains("Історія"));
    }
}
