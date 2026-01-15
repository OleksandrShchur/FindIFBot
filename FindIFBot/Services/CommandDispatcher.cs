using FindIFBot.Domain;
using FindIFBot.Handlers;
using FindIFBot.Helpers;
using FindIFBot.Persistence;
using FindIFBot.Services.Admin;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Services
{
    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly ITelegramBotClient _bot;
        private readonly IUserSessionRepository _sessions;
        private readonly IMessageStore _messages;
        private readonly IAdminWorkflowService _admin;
        private readonly IStartHandler _startHandler;
        private static readonly Dictionary<string, List<Message>> _mediaBuffer = new();
        private static readonly object _lock = new();

        public CommandDispatcher(
            ITelegramBotClient bot,
            IUserSessionRepository sessions,
            IMessageStore messages,
            IAdminWorkflowService admin,
            IStartHandler startHandler)
        {
            _bot = bot;
            _sessions = sessions;
            _messages = messages;
            _admin = admin;
            _startHandler = startHandler;
        }

        public async Task DispatchAsync(Update update)
        {
            if (update.CallbackQuery != null)
            {
                await _admin.HandleCallbackAsync(update.CallbackQuery);
                return;
            }
            if (update.Message != null)
            {
                await HandleMessageAsync(update.Message);
            }
        }

        private async Task HandleMessageAsync(Message message)
        {
            var userId = message.From?.Id ?? message.Chat.Id;
            var session = _sessions.Get(userId);

            if (message.MediaGroupId != null)
            {
                var mediaGroupId = message.MediaGroupId!;
                var key = MediaKey(userId, mediaGroupId);
                bool isFirstMessageInGroup = false;
                lock (_lock)
                {
                    if (!_mediaBuffer.TryGetValue(key, out var list))
                    {
                        list = new List<Message>();
                        _mediaBuffer[key] = list;
                        isFirstMessageInGroup = true;
                    }
                    list.Add(message);
                }
                if (isFirstMessageInGroup)
                {
                    _ = Task.Delay(2000).ContinueWith(async _ =>
                    {
                        List<Message>? group;
                        lock (_lock)
                        {
                            if (!_mediaBuffer.TryGetValue(key, out group))
                                return;
                            _mediaBuffer.Remove(key);
                        }
                        if (group == null || group.Count == 0)
                            return;

                        var processingUserId = group[0].From!.Id;
                        var currentSession = _sessions.Get(processingUserId);
                        await HandleMediaGroupAsync(group, currentSession);
                    });
                }
                return;
            }

            await HandleSingleMessageAsync(message, session);
        }

        private async Task HandleMediaGroupAsync(List<Message> messages, UserSession session)
        {
            var orderedMessages = messages.OrderBy(m => m.MessageId).ToList();

            var captionMessage = orderedMessages.FirstOrDefault(m => !string.IsNullOrEmpty(m.Caption))
                                 ?? orderedMessages[0];

            var chatId = captionMessage.Chat.Id;

            // Безпечний збір тільки фото
            var photos = orderedMessages
                .Where(m => m.Photo != null)
                .Select(m => m.Photo.Last().FileId)
                .ToList();

            var totalMediaCount = orderedMessages.Count;
            var ignoredCount = totalMediaCount - photos.Count;

            bool isSubmissionState = session.State == UserState.WaitingForFindQuery || session.State == UserState.WaitingForAdContent;

            if (isSubmissionState)
            {
                // Валідація кількості
                if (photos.Count > 10)
                {
                    await _bot.SendMessage(
                        chatId,
                        "Помилка: забагато фотографій (максимум 10 в одному альбомі). Будь ласка, надішліть менше.",
                        replyMarkup: Keyboards.DefaultMarkup()
                    );
                    session.State = UserState.Idle;
                    _sessions.Save(session);
                    return;
                }

                // Якщо є ігноровані медіа — попередження
                if (ignoredCount > 0)
                {
                    await _bot.SendMessage(
                        chatId,
                        $"Увага: з {totalMediaCount} елементів альбому оброблено тільки {photos.Count} фото. Відео, гіфки та інші медіа ігноруються.",
                        replyMarkup: Keyboards.DefaultMarkup()
                    );
                }

                // Якщо зовсім немає фото — відхилити
                if (photos.Count == 0)
                {
                    await _bot.SendMessage(
                        chatId,
                        "Помилка: в альбомі немає фото. Надішліть альбом з фотографіями.",
                        replyMarkup: Keyboards.DefaultMarkup()
                    );
                    session.State = UserState.Idle;
                    _sessions.Save(session);
                    return;
                }
            }

            var userId = captionMessage.From!.Id;

            var stored = new StoredMessage(
                ChatId: chatId,
                UserId: userId,
                Text: captionMessage.Caption,
                Photos: photos,
                MediaGroupId: captionMessage.MediaGroupId
            );

            _messages.Store(captionMessage.MessageId, stored);

            switch (session.State)
            {
                case UserState.WaitingForFindQuery:
                    await _admin.SubmitFindAsync(captionMessage);
                    session.State = UserState.Idle;
                    _sessions.Save(session);
                    break;
                case UserState.WaitingForAdContent:
                    await _admin.SubmitAdAsync(captionMessage);
                    session.State = UserState.Idle;
                    _sessions.Save(session);
                    break;
            }
        }

        private async Task HandleSingleMessageAsync(Message message, UserSession session)
        {
            var userId = message.From?.Id ?? message.Chat.Id;

            var text = message.Text?.Trim() ?? message.Caption?.Trim();

            var photos = message.Photo != null
                ? new List<string> { message.Photo.Last().FileId }
                : new List<string>();

            var stored = new StoredMessage(
                message.Chat.Id,
                userId,
                text,
                photos,
                message.MediaGroupId
            );

            _messages.Store(message.MessageId, stored);

            var normalized = (text ?? string.Empty).ToLowerInvariant();

            // === НОВА ВАЛІДАЦІЯ ДЛЯ ОДИНОЧНОГО ПОВІДОМЛЕННЯ ===
            bool isSubmissionState = session.State == UserState.WaitingForFindQuery || session.State == UserState.WaitingForAdContent;

            if (isSubmissionState)
            {
                bool hasNonPhotoMedia = (message.Video != null || message.Animation != null || message.Document != null || message.Audio != null || message.Voice != null || message.Sticker != null);

                if (hasNonPhotoMedia)
                {
                    await _bot.SendMessage(
                        message.Chat.Id,
                        "Помилка: надіслано не фото (відео, документ тощо). Підтримуємо тільки фотографії.",
                        replyMarkup: Keyboards.DefaultMarkup()
                    );
                    session.State = UserState.Idle;
                    _sessions.Save(session);
                    return;
                }
            }
            // === КІНЕЦЬ ВАЛІДАЦІЇ ===

            if (normalized == "/start")
            {
                session.State = UserState.Idle;
                _sessions.Save(session);
                await _startHandler.HandleAsync(_bot, message);
                return;
            }

            switch (session.State)
            {
                case UserState.WaitingForFindQuery:
                    await _admin.SubmitFindAsync(message);
                    session.State = UserState.Idle;
                    _sessions.Save(session);
                    return;
                case UserState.WaitingForAdContent:
                    await _admin.SubmitAdAsync(message);
                    session.State = UserState.Idle;
                    _sessions.Save(session);
                    return;
                case UserState.WaitingForAdvice:
                    await HandleAdviceAsync(message);
                    session.State = UserState.Idle;
                    _sessions.Save(session);
                    return;
            }

            if (IsFindCommand(normalized))
            {
                session.State = UserState.WaitingForFindQuery;
                _sessions.Save(session);
                await _bot.SendMessage(
                    message.Chat.Id,
                    new FindHandler().Handle(),
                    replyMarkup: new ReplyKeyboardRemove()
                );
                return;
            }
            if (IsAdsCommand(normalized))
            {
                session.State = UserState.WaitingForAdContent;
                _sessions.Save(session);
                await _bot.SendMessage(
                    message.Chat.Id,
                    new AdsHandler().Handle(),
                    replyMarkup: new ReplyKeyboardRemove()
                );
                return;
            }
            if (IsAdviceCommand(normalized))
            {
                session.State = UserState.WaitingForAdvice;
                _sessions.Save(session);
                await _bot.SendMessage(
                    message.Chat.Id,
                    new IdeasHandler().Handle(),
                    replyMarkup: new ReplyKeyboardRemove()
                );
                return;
            }

            await HandleStatelessCommandAsync(message, normalized);
        }

        // Решта методів без змін...
        private async Task HandleAdviceAsync(Message message)
        {
            await _bot.SendMessage(
                message.Chat.Id,
                "Дякуємо за вашу ідею. Ми її опрацюємо.",
                replyMarkup: Keyboards.DefaultMarkup()
            );
        }

        private async Task HandleStatelessCommandAsync(Message message, string normalized)
        {
            ICommandHandler handler = normalized switch
            {
                "/help" or "довідка" => new HelpHandler(),
                "/ads_rule" or "/ads-rules" or "правила розміщення реклами" => new AdsRulesHandler(),
                "/donate" or "підтримати нас" => new SupportUsHandler(),
                _ => new UnknownHandler()
            };
            await _bot.SendMessage(
                message.Chat.Id,
                handler.Handle(),
                replyMarkup: Keyboards.DefaultMarkup()
            );
        }

        private static bool IsFindCommand(string normalized) =>
            normalized == "/find" || normalized == "розпочати пошук";
        private static bool IsAdsCommand(string normalized) =>
            normalized == "/ads" || normalized == "розмістити рекламу";
        private static bool IsAdviceCommand(string normalized) =>
            normalized == "/advice" || normalized == "запропонувати покращення";

        private static string MediaKey(long userId, string mediaGroupId) =>
            $"{userId}:{mediaGroupId}";
    }
}