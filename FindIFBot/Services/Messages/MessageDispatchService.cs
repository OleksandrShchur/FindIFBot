using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Handlers;
using FindIFBot.Helpers;
using FindIFBot.Helpers.Logs;
using FindIFBot.Services.Ask;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Services.Messages
{
    public class MessageDispatchService : IMessageDispatchService
    {
        private const string Component = "MessageDispatch";
        private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };

        private readonly ITelegramBotClient _bot;
        private readonly IUserSessionRepository _sessions;
        private readonly IUserRequestHistoryRepository _history;
        private readonly IAsyncCommandHandler _startHandler;
        private readonly IMediaGroupHandler _mediaGroupHandler;
        private readonly IMessageStorageService _storage;
        private readonly ISubmissionValidator _validator;
        private readonly IAskConfirmationService _confirmation;
        private readonly IAskFlowService _askFlow;
        private readonly IMessageCommandRouter _commandRouter;
        private readonly TelegramOptions _telegram;
        private readonly IAppLogger<MessageDispatchService> _logger;

        public MessageDispatchService(
            ITelegramBotClient bot,
            IUserSessionRepository sessions,
            IUserRequestHistoryRepository history,
            IEnumerable<IAsyncCommandHandler> handlers,
            IMediaGroupHandler mediaGroupHandler,
            IMessageStorageService storage,
            ISubmissionValidator validator,
            IAskConfirmationService confirmation,
            IAskFlowService askFlow,
            IMessageCommandRouter commandRouter,
            IOptions<TelegramOptions> telegram,
            IAppLogger<MessageDispatchService> logger)
        {
            _bot = bot;
            _sessions = sessions;
            _history = history;
            _startHandler = handlers.OfType<StartHandler>().Single();
            _mediaGroupHandler = mediaGroupHandler;
            _storage = storage;
            _validator = validator;
            _confirmation = confirmation;
            _askFlow = askFlow;
            _commandRouter = commandRouter;
            _telegram = telegram.Value;
            _logger = logger;
        }

        public async Task HandleAsync(Message message)
        {
            var userId = message.From?.Id ?? message.Chat.Id;

            if (message.MediaGroupId != null)
            {
                await _mediaGroupHandler.BufferAsync(message, userId);
                return;
            }

            var session = await _sessions.GetAsync(userId);
            await HandleSingleMessageAsync(message, session);
        }

        private async Task HandleSingleMessageAsync(Message message, UserSession session)
        {
            var userId = message.From?.Id ?? message.Chat.Id;
            var hasHistory = await _history.HasHistory(userId);
            // Keep original Telegram text so entity offsets stay valid for text_link / formatting.
            var text = message.Text ?? message.Caption;
            var photos = message.Photo != null
                ? new List<string> { message.Photo.Last().FileId }
                : new List<string>();

            var normalized = BotCommands.Normalize(text);

            // Ask prompt keeps an inline «Назад у Головне меню» button; the reply keyboard may still be
            // visible. Menu taps must exit the wait state, not become ask content.
            if (session.State == UserState.WaitingForAskQuery && BotCommands.IsMenuCommand(normalized))
            {
                session.State = UserState.Idle;
                await _sessions.SaveAsync(session);
            }

            if (session.State == UserState.WaitingForAskQuery)
            {
                var validation = _validator.ValidateSingleMessage(message, text, photos.Count);
                if (!validation.IsValid)
                {
                    await SendValidationErrorAsync(message, session, validation.ErrorMessage!, hasHistory);
                    return;
                }
            }

            var stored = await _storage.StoreSingleAsync(message, text, photos);

            await _logger.LogInfo(Component,
                $"Stored single message | UserId: {userId} | MessageId: {stored.MessageId} | Photos: {photos.Count} | TextLength: {(text?.Length ?? 0)}");

            if (BotCommands.IsStart(normalized))
            {
                session.State = UserState.Idle;
                await _sessions.SaveAsync(session);
                await _startHandler.HandleAsync(_bot, message);
                return;
            }

            if (session.State == UserState.WaitingForAskQuery)
            {
                await _confirmation.SendConfirmationAsync(message, session);
                return;
            }

            if (BotCommands.IsAsk(normalized))
            {
                await _askFlow.StartAsync(message.Chat.Id, userId, session);
                return;
            }

            await _commandRouter.RouteAsync(message, normalized);
        }

        private async Task SendValidationErrorAsync(
            Message message,
            UserSession session,
            string errorMessage,
            bool hasHistory)
        {
            var userId = message.From?.Id ?? message.Chat.Id;
            await _logger.LogWarning(Component,
                $"Validation failed for single message | UserId: {userId} | MessageId: {message.MessageId}");

            await _bot.SendMessage(
                message.Chat.Id,
                errorMessage,
                replyMarkup: Keyboards.GetKeyboard(hasHistory, userId == _telegram.AdminId),
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html
            );

            session.State = UserState.Idle;
            await _sessions.SaveAsync(session);
        }
    }
}
