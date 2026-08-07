using FindIFBot.EF.Repositories;
using FindIFBot.Helpers.Logs;
using FindIFBot.Persistence;
using FindIFBot.Services.Admin;
using FindIFBot.Services.Ask;
using FindIFBot.UnitTests.TestSupport;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FindIFBot.UnitTests.Services.Admin
{
    public class AdminWorkflowServiceTests
    {
        private const long UserId = 321;
        private const int MessageId = 99;

        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly IMessageStore _messages = Substitute.For<IMessageStore>();
        private readonly IUserSessionRepository _sessions = Substitute.For<IUserSessionRepository>();
        private readonly ICallbackAuthorizationService _authorization = Substitute.For<ICallbackAuthorizationService>();
        private readonly IAdminModerationService _moderation = Substitute.For<IAdminModerationService>();
        private readonly IAskUnexpectedErrorNotifier _unexpectedError = Substitute.For<IAskUnexpectedErrorNotifier>();
        private readonly IAppLogger<AdminWorkflowService> _logger = Substitute.For<IAppLogger<AdminWorkflowService>>();
        private readonly AdminWorkflowService _sut;

        public AdminWorkflowServiceTests()
        {
            _authorization.IsAuthorizedAsync(Arg.Any<CallbackQuery>(), Arg.Any<AdminCallbackData>())
                .Returns(true);
            _messages.TryGetAsync(MessageId)
                .Returns(new StoredMessage(UserId, UserId, "promo text", [], null, MessageId));

            _sut = new AdminWorkflowService(
                _bot, _messages, _sessions, new AdminCallbackParser(), _authorization, _moderation, _unexpectedError, _logger);
        }

        [Fact]
        public async Task HandleCallbackAsync_AdvertisementAction_RoutesToModerationAndCleansUp()
        {
            var callback = TelegramBuilder.CallbackQuery($"!ask|{UserId}|{MessageId}");

            await _sut.HandleCallbackAsync(callback);

            await _moderation.Received(1).MarkAdvertisementAsync(UserId, MessageId);
            await _messages.Received(1).RemoveAsync(MessageId);
        }

        [Fact]
        public async Task HandleCallbackAsync_AdvertisementAction_DoesNotApproveRejectOrDuplicate()
        {
            var callback = TelegramBuilder.CallbackQuery($"!ask|{UserId}|{MessageId}");

            await _sut.HandleCallbackAsync(callback);

            await _moderation.DidNotReceive().PublishAsync(Arg.Any<long>(), Arg.Any<StoredMessage>());
            await _moderation.DidNotReceive().RejectAsync(Arg.Any<long>(), Arg.Any<int>());
            await _moderation.DidNotReceive().MarkDuplicateAsync(Arg.Any<long>(), Arg.Any<int>());
        }

        [Fact]
        public async Task HandleCallbackAsync_NeedsAttentionAction_RoutesToModerationAndCleansUp()
        {
            var callback = TelegramBuilder.CallbackQuery($"*ask|{UserId}|{MessageId}");

            await _sut.HandleCallbackAsync(callback);

            await _moderation.Received(1).MarkNeedsAttentionAsync(UserId, MessageId);
            await _messages.Received(1).RemoveAsync(MessageId);
        }

        [Fact]
        public async Task HandleCallbackAsync_NeedsAttentionAction_DoesNotApproveRejectDuplicateOrAds()
        {
            var callback = TelegramBuilder.CallbackQuery($"*ask|{UserId}|{MessageId}");

            await _sut.HandleCallbackAsync(callback);

            await _moderation.DidNotReceive().PublishAsync(Arg.Any<long>(), Arg.Any<StoredMessage>());
            await _moderation.DidNotReceive().RejectAsync(Arg.Any<long>(), Arg.Any<int>());
            await _moderation.DidNotReceive().MarkDuplicateAsync(Arg.Any<long>(), Arg.Any<int>());
            await _moderation.DidNotReceive().MarkAdvertisementAsync(Arg.Any<long>(), Arg.Any<int>());
        }

        [Fact]
        public async Task HandleCallbackAsync_ProceedThrows_NotifiesUnexpectedError()
        {
            _moderation.SubmitAskAsync(Arg.Any<StoredMessage>(), Arg.Any<FindIFBot.Domain.UserInfo>())
                .Returns<Task>(_ => throw new InvalidOperationException("submit failed"));
            var callback = TelegramBuilder.CallbackQuery($"proceed|{UserId}|{MessageId}", userId: UserId, chatId: 400);

            await _sut.HandleCallbackAsync(callback);

            await _unexpectedError.Received(1).NotifyAsync(400, UserId);
            await _logger.Received().LogError("AdminWorkflow", Arg.Is<string>(m => m.Contains("proceed")));
        }

        [Fact]
        public async Task HandleCallbackAsync_CancelThrows_NotifiesUnexpectedError()
        {
            _moderation.CancelAskAsync(UserId, MessageId)
                .Returns<Task>(_ => throw new InvalidOperationException("cancel failed"));
            var callback = TelegramBuilder.CallbackQuery($"cancel|{UserId}|{MessageId}", userId: UserId, chatId: 400);

            await _sut.HandleCallbackAsync(callback);

            await _unexpectedError.Received(1).NotifyAsync(400, UserId);
            await _logger.Received().LogError("AdminWorkflow", Arg.Is<string>(m => m.Contains("cancel")));
        }
    }
}
