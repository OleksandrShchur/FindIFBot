using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers.Logs;
using FindIFBot.Persistence;
using FindIFBot.Services.Ask;
using FindIFBot.Services.Messages;
using FindIFBot.UnitTests.TestSupport;
using Telegram.Bot;

namespace FindIFBot.UnitTests.Services.Messages
{
    public class AskConfirmationServiceTests
    {
        private const long UserId = 100;
        private const long ChatId = 200;
        private const int MessageId = 55;

        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly IMessageStore _messages = Substitute.For<IMessageStore>();
        private readonly IUserSessionRepository _sessions = Substitute.For<IUserSessionRepository>();
        private readonly IAskUnexpectedErrorNotifier _unexpectedError = Substitute.For<IAskUnexpectedErrorNotifier>();
        private readonly IAppLogger<AskConfirmationService> _logger = Substitute.For<IAppLogger<AskConfirmationService>>();
        private readonly AskConfirmationService _sut;

        public AskConfirmationServiceTests()
        {
            _sut = new AskConfirmationService(_bot, _messages, _sessions, _unexpectedError, _logger);
        }

        [Fact]
        public async Task SendConfirmationAsync_WhenStoredMessageMissing_NotifiesUnexpectedError()
        {
            _messages.TryGetAsync(MessageId).Returns((StoredMessage?)null);
            var message = TelegramBuilder.TextMessage("hello", userId: UserId, chatId: ChatId, messageId: MessageId);
            var session = new UserSession { UserId = UserId, State = UserState.WaitingForAskQuery };

            await _sut.SendConfirmationAsync(message, session);

            await _unexpectedError.Received(1).NotifyAsync(ChatId, UserId);
            await _sessions.DidNotReceive().SaveAsync(Arg.Any<UserSession>());
            await _logger.Received().LogError("AskConfirmation", Arg.Is<string>(m => m.Contains("not found")));
        }
    }
}
