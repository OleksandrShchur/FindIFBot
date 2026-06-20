using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers;
using FindIFBot.Helpers.Logs;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Services.Messages
{
    public class MediaGroupHandler : IMediaGroupHandler
    {
        private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };
        private const string Component = "MediaGroup";

        private readonly ITelegramBotClient _bot;
        private readonly IUserSessionRepository _sessions;
        private readonly IMediaGroupBuffer _buffer;
        private readonly IMediaGroupQueue _queue;
        private readonly IMessageStorageService _storage;
        private readonly ISubmissionValidator _validator;
        private readonly IAskConfirmationService _confirmation;
        private readonly IMessageCommandRouter _commandRouter;
        private readonly IAppLogger<MediaGroupHandler> _logger;

        public MediaGroupHandler(
            ITelegramBotClient bot,
            IUserSessionRepository sessions,
            IMediaGroupBuffer buffer,
            IMediaGroupQueue queue,
            IMessageStorageService storage,
            ISubmissionValidator validator,
            IAskConfirmationService confirmation,
            IMessageCommandRouter commandRouter,
            IAppLogger<MediaGroupHandler> logger)
        {
            _bot = bot;
            _sessions = sessions;
            _buffer = buffer;
            _queue = queue;
            _storage = storage;
            _validator = validator;
            _confirmation = confirmation;
            _commandRouter = commandRouter;
            _logger = logger;
        }

        public async Task BufferAsync(Message message, long userId)
        {
            var mediaGroupId = message.MediaGroupId!;
            var isFirstMessageInGroup = _buffer.Add(userId, mediaGroupId, message);

            await _logger.LogInfo(Component,
                $"Media group buffer updated | UserId: {userId} | MediaGroupId: {mediaGroupId} | AddedMessageId: {message.MessageId} | IsFirst: {isFirstMessageInGroup}");

            if (!isFirstMessageInGroup)
            {
                return;
            }

            await _queue.EnqueueAsync(new MediaGroupWorkItem(userId, mediaGroupId));
        }

        public async Task ProcessAsync(
            List<Message> messages,
            UserSession session,
            IUserRequestHistoryRepository history)
        {
            var orderedMessages = messages.OrderBy(m => m.MessageId).ToList();
            var captionMessage = orderedMessages.FirstOrDefault(m => !string.IsNullOrEmpty(m.Caption))
                                 ?? orderedMessages[0];

            var userId = captionMessage.From!.Id;
            var hasHistory = await history.HasHistory(userId);

            var photos = orderedMessages
                .Where(m => m.Photo != null)
                .Select(m => m.Photo!.Last().FileId)
                .ToList();

            var totalMediaCount = orderedMessages.Count;
            var ignoredCount = totalMediaCount - photos.Count;

            if (session.State == UserState.WaitingForAskQuery)
            {
                if (ignoredCount > 0)
                {
                    await _logger.LogWarning(Component,
                        $"Ignored non-photo media in album | UserId: {userId} | Ignored: {ignoredCount} | Total: {totalMediaCount}");

                    await _bot.SendMessage(
                        captionMessage.Chat.Id,
                        "⚠️ <b>Увага:</b> в альбомі є не-фото елементи\n\n" +
                        $"З {totalMediaCount} файлів оброблено тільки <b>{photos.Count} фото</b>.\n" +
                        "Відео, GIF, документи та інші типи <b>ігноруються</b>.",
                        replyMarkup: Keyboards.GetKeyboard(hasHistory),
                        linkPreviewOptions: NoPreview,
                        parseMode: ParseMode.Html
                    );
                }

                var validation = _validator.ValidateMediaGroup(
                    orderedMessages,
                    photos.Count,
                    ignoredCount,
                    captionMessage.Caption ?? string.Empty);

                if (!validation.IsValid)
                {
                    await SendValidationErrorAsync(captionMessage, session, validation.ErrorMessage!, hasHistory);
                    return;
                }
            }

            var stored = await _storage.StoreMediaGroupAsync(captionMessage, photos);

            await _logger.LogInfo(Component,
                $"Stored media group | UserId: {userId} | MessageId: {stored.MessageId} | " +
                $"Photos: {photos.Count} | Ignored: {ignoredCount} | CaptionLength: {(stored.Text?.Length ?? 0)}");

            if (session.State == UserState.WaitingForAskQuery)
            {
                await _confirmation.SendConfirmationAsync(captionMessage, session);
                return;
            }

            var normalized = (stored.Text ?? string.Empty).ToLowerInvariant();
            await _commandRouter.RouteAsync(captionMessage, normalized);
        }

        private async Task SendValidationErrorAsync(
            Message message,
            UserSession session,
            string errorMessage,
            bool hasHistory)
        {
            await _logger.LogWarning(Component,
                $"Validation failed for media group | UserId: {message.From!.Id} | MessageId: {message.MessageId}");

            await _bot.SendMessage(
                message.Chat.Id,
                errorMessage,
                replyMarkup: Keyboards.GetKeyboard(hasHistory),
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html
            );

            session.State = UserState.Idle;
            await _sessions.SaveAsync(session);
        }
    }
}
