using FindIFBot.Domain;
using FindIFBot.EF;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Handlers;
using FindIFBot.Helpers;
using FindIFBot.Helpers.Logs;
using FindIFBot.Persistence;
using FindIFBot.Services.Admin;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Services
{
    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly ITelegramBotClient _bot;
        private readonly IUserSessionRepository _sessions;
        private readonly IMessageStore _messages;
        private readonly IAdminWorkflowService _admin;
        private readonly IAsyncCommandHandler _startHandler;
        private readonly IAsyncCommandHandler _historyHandler;
        private readonly IUserRequestHistoryRepository _history;
        private readonly IAppLogger<CommandDispatcher> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private static readonly Dictionary<string, List<Message>> _mediaBuffer = new();
        private static readonly object _lock = new();
        private const string Component = "Dispatcher";

        public CommandDispatcher(
            ITelegramBotClient bot,
            IUserSessionRepository sessions,
            IMessageStore messages,
            IAdminWorkflowService admin,
            IEnumerable<IAsyncCommandHandler> handlers,
            IUserRequestHistoryRepository history,
            IAppLogger<CommandDispatcher> logger,
            IServiceScopeFactory scopeFactory)
        {
            _bot = bot;
            _sessions = sessions;
            _messages = messages;
            _admin = admin;
            _startHandler = handlers.OfType<StartHandler>().Single();
            _historyHandler = handlers.OfType<HistoryHandler>().Single();
            _history = history;
            _logger = logger;
            _scopeFactory = scopeFactory;
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
                /*
                if (update.Message.MediaGroupId != null)
                {
                    var messageText = $"{update.Message.Caption}\n\n\n" +
                        $"From: {update.Message.From.Id}\n" +
                        $"MessageId: {update.Message.Id}\n" +
                        $"MediaGroupId: {update.Message.MediaGroupId}\n" +
                        $"UserName: @{update.Message.From.Username}\n" +
                        $"FirstName: {update.Message.From.FirstName}\n" +
                        $"LastName: {update.Message.From.LastName}";
                    await _bot.SendMediaGroup
                    (
                        _options.LogsOutputChannel,
                        media: update.Message.Photo != null
                            ? new[] { new InputMediaPhoto(update.Message.Photo.Last().FileId) { Caption = messageText } }
                            : new[] { new InputMediaPhoto("") { Caption = messageText } },
                        messageThreadId: _options.AllMessagesThreadId
                    );
                }
                else
                {
                    var messageText = $"{update.Message.Text}\n\n\n" +
                        $"From: {update.Message.From.Id}\n" +
                        $"MessageId: {update.Message.Id}\n" +
                        $"UserName: @{update.Message.From.Username}\n" +
                        $"FirstName: {update.Message.From.FirstName}\n" +
                        $"LastName: {update.Message.From.LastName}";
                    await _bot.SendMessage
                    (
                        _options.LogsOutputChannel,
                        messageText,
                        messageThreadId: _options.AllMessagesThreadId
                    );
                }
                */

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

                await _logger.LogInfo(Component,
                    $"Media group buffer updated | UserId: {userId} | MediaGroupId: {mediaGroupId} | AddedMessageId: {message.MessageId} | IsFirst: {isFirstMessageInGroup}");

                if (isFirstMessageInGroup)
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(2000);

                        List<Message>? group = null;
                        long capturedUserId = message.From!.Id;
                        string capturedMediaGroupId = mediaGroupId;

                        lock (_lock)
                        {
                            var keyLocal = MediaKey(capturedUserId, capturedMediaGroupId);
                            if (!_mediaBuffer.TryGetValue(keyLocal, out group) || group == null || group.Count == 0)
                            {
                                _logger.LogWarning(Component,
                                    $"Media group buffer empty or removed | user={capturedUserId} | groupId={capturedMediaGroupId}");
                                return;
                            }
                            _mediaBuffer.Remove(keyLocal);
                        }

                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
                            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
                            var freshHistory = scope.ServiceProvider.GetRequiredService<IUserRequestHistoryRepository>();

                            var currentSession = sessions.Get(capturedUserId);

                            if (currentSession == null)
                            {
                                await _logger.LogError(Component, $"No session found in media group background task | user={capturedUserId}");
                                
                                return;
                            }

                            await _logger.LogInfo(Component,
                                $"Processing media group in fresh scope | user={capturedUserId} | state={currentSession.State} | photos={group.Count}");

                            await HandleMediaGroupAsync(group, currentSession, freshHistory);
                        }
                        catch (Exception ex)
                        {
                            await _logger.LogError(Component,
                                $"Media group background task failed | user={capturedUserId} | ex={ex.Message}\n{ex.StackTrace}");
                        }
                    });
                }
                return;
            }

            await HandleSingleMessageAsync(message, session);
        }

        private async Task HandleMediaGroupAsync(
            List<Message> messages,
            UserSession session,
            IUserRequestHistoryRepository freshHistory)
        {
            var orderedMessages = messages.OrderBy(m => m.MessageId).ToList();
            var captionMessage = orderedMessages.FirstOrDefault(m => !string.IsNullOrEmpty(m.Caption))
                                 ?? orderedMessages[0];

            var chatId = captionMessage.Chat.Id;
            var userId = captionMessage.From!.Id;

            var hasHistory = await freshHistory.HasHistory(userId);

            var photos = orderedMessages
                .Where(m => m.Photo != null)
                .Select(m => m.Photo!.Last().FileId)
                .ToList();

            var totalMediaCount = orderedMessages.Count;
            var ignoredCount = totalMediaCount - photos.Count;

            bool isSubmissionState = session.State == UserState.WaitingForAskQuery;

            if (isSubmissionState)
            {
                if (photos.Count > 10)
                {
                    await _logger.LogWarning(Component,
                        $"Validation failed: too many photos | UserId: {userId} | Photos: {photos.Count}");

                    await _bot.SendMessage(
                        chatId,
                        "❌ <b>Помилка:</b> забагато фотографій\n" +
                        $"<b>Максимум дозволено:</b> 10 фото в одному запиті\n\n" +
                        "Будь ласка, надішліть менше.",
                        replyMarkup: Keyboards.GetKeyboard(hasHistory),
                        parseMode: ParseMode.Html
                    );

                    session.State = UserState.Idle;
                    _sessions.Save(session);
                    return;
                }

                if (ignoredCount > 0)
                {
                    await _logger.LogWarning(Component,
                        $"Ignored non-photo media in album | UserId: {userId} | Ignored: {ignoredCount} | Total: {totalMediaCount}");

                    await _bot.SendMessage(
                        chatId,
                        "⚠️ <b>Увага:</b> в альбомі є не-фото елементи\n\n" +
                        $"З {totalMediaCount} файлів оброблено тільки <b>{photos.Count} фото</b>.\n" +
                        "Відео, GIF, документи та інші типи <b>ігноруються</b>.",
                        replyMarkup: Keyboards.GetKeyboard(hasHistory),
                        parseMode: ParseMode.Html
                    );
                }

                if (photos.Count == 0)
                {
                    await _logger.LogWarning(Component,
                        $"Validation failed: no photos in album | UserId: {userId}");

                    await _bot.SendMessage(
                        chatId,
                        "❌ <b>Помилка:</b> в альбомі немає фотографій\n\n" +
                        "Надішліть, будь ласка, альбом саме з фото.",
                        replyMarkup: Keyboards.GetKeyboard(hasHistory),
                        parseMode: ParseMode.Html
                    );

                    session.State = UserState.Idle;
                    _sessions.Save(session);
                    return;
                }
            }

            var stored = new StoredMessage(
                ChatId: chatId,
                UserId: userId,
                Text: captionMessage.Caption,
                Photos: photos,
                MediaGroupId: captionMessage.MediaGroupId,
                MessageId: captionMessage.MessageId
            );

            _messages.Store(captionMessage.MessageId, stored);

            await _logger.LogInfo(Component,
                $"Stored media group | UserId: {userId} | MessageId: {captionMessage.MessageId} | " +
                $"Photos: {photos.Count} | Ignored: {ignoredCount} | CaptionLength: {(captionMessage.Caption?.Length ?? 0)}");

            switch (session.State)
            {
                case UserState.WaitingForAskQuery:
                    await PrepareAskConfirmationAsync(captionMessage, session);
                    break;
            }
        }

        private async Task HandleSingleMessageAsync(Message message, UserSession session)
        {
            var userId = message.From?.Id ?? message.Chat.Id;
            var hasHistory = await _history.HasHistory(userId);
            var text = message.Text?.Trim() ?? message.Caption?.Trim();
            var photos = message.Photo != null
                ? new List<string> { message.Photo.Last().FileId }
                : new List<string>();
            var stored = new StoredMessage(
                message.Chat.Id,
                userId,
                text,
                photos,
                message.MediaGroupId,
                message.MessageId
            );
            _messages.Store(message.MessageId, stored);

            await _logger.LogInfo(Component,
                $"Stored single message | UserId: {userId} | MessageId: {message.MessageId} | Photos: {photos.Count} | TextLength: {(text?.Length ?? 0)}");
            
            var normalized = (text ?? string.Empty).ToLowerInvariant();
            bool isSubmissionState = session.State == UserState.WaitingForAskQuery;
            
            if (isSubmissionState)
            {
                bool hasNonPhotoMedia = (message.Video != null || message.Animation != null || message.Document != null ||
                                         message.Audio != null || message.Voice != null || message.Sticker != null);
                if (hasNonPhotoMedia)
                {
                    await _logger.LogWarning(Component,
                        $"Validation failed: non-photo media in submission | UserId: {userId} | MessageId: {message.MessageId}");
                    await _bot.SendMessage(
                        message.Chat.Id,
                        "❌ <b>Помилка:</b> надіслано не фото\n\n" +
                        "Ми підтримуємо <b>тільки фотографії</b>.\n" +
                        "Відео, документи, GIF, стікери та інші типи файлів зараз не обробляються.",
                        replyMarkup: Keyboards.GetKeyboard(hasHistory),
                        parseMode: ParseMode.Html
                    );
                    session.State = UserState.Idle;
                    _sessions.Save(session);
                    return;
                }
            }
            if (normalized == "/start")
            {
                session.State = UserState.Idle;
                _sessions.Save(session);
                await _startHandler.HandleAsync(_bot, message);

                return;
            }
            switch (session.State)
            {
                case UserState.WaitingForAskQuery:
                    await PrepareAskConfirmationAsync(message, session);

                    return;
            }
            if (IsAskCommand(normalized))
            {
                session.State = UserState.WaitingForAskQuery;
                _sessions.Save(session);
                await _logger.LogInfo(Component, $"User started ask flow | UserId: {userId}");
                await _bot.SendMessage(
                    message.Chat.Id,
                    new AskHandler().Handle(),
                    replyMarkup: new ReplyKeyboardRemove(),
                    parseMode: ParseMode.Html
                );

                return;
            }

            await HandleStatelessCommandAsync(message, normalized);
        }

        private async Task HandleStatelessCommandAsync(Message message, string normalized)
        {
            var userId = message.From!.Id;
            var hasHistory = await _history.HasHistory(userId);
            if (normalized == "📋 історія запитів" || normalized == "/history")
            {
                await _historyHandler.HandleAsync(_bot, message);
                return;
            }
            ICommandHandler handler = normalized switch
            {
                "/help" or "ℹ️ довідка" => new HelpHandler(),
                "/support" or "❤️ підтримати нас" => new SupportUsHandler(),
                _ => new UnknownHandler()
            };

            await _logger.LogInfo(Component, $"Stateless command handled: {normalized} | UserId: {userId}");
            await _bot.SendMessage(
                message.Chat.Id,
                handler.Handle(),
                replyMarkup: Keyboards.GetKeyboard(hasHistory),
                parseMode: ParseMode.Html
            );
        }

        private static bool IsAskCommand(string normalized) =>
            normalized == "/ask" || normalized == "📨 надіслати запит";

        private async Task PrepareAskConfirmationAsync(Message message, UserSession session)
        {
            await _logger.LogInfo(Component,
                $"Preparing ask confirmation | UserId: {message.From!.Id} | MessageId: {message.MessageId}");

            if (!_messages.TryGet(message.MessageId, out var stored))
            {
                await _logger.LogError(Component,
                    $"Stored message not found for confirmation | UserId: {message.From!.Id} | MessageId: {message.MessageId}");
                return;
            }

            // Send preview of the content
            if (stored.Photos.Count > 0)
            {
                var media = stored.Photos
                    .Select((id, i) => new InputMediaPhoto(id)
                    {
                        Caption = i == 0 ? stored.Text : null
                    })
                    .ToArray();

                await _bot.SendMediaGroup(message.Chat.Id, media);
            }
            else
            {
                // If no photos — send just text (with fallback if empty)
                var previewText = string.IsNullOrWhiteSpace(stored.Text)
                    ? "📝 (тільки текст без вмісту)"
                    : stored.Text;

                await _bot.SendMessage(
                    message.Chat.Id,
                    previewText,
                    parseMode: ParseMode.Html
                );
            }

            // Confirmation message + buttons
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Надіслати", $"proceed|{message.From!.Id}|{message.MessageId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Скасувати", $"cancel|{message.From!.Id}|{message.MessageId}")
                }
            });

            await _bot.SendMessage(
                message.Chat.Id,
                "📤 <b>Надіслати цей запит адмінам на перевірку?</b>\n\n",
                replyMarkup: keyboard,
                parseMode: ParseMode.Html
            );

            await _logger.LogInfo(Component,
                $"Ask confirmation sent | UserId: {message.From!.Id} | MessageId: {message.MessageId} | Photos: {stored.Photos.Count}");

            session.State = UserState.ConfirmAskContent;
            _sessions.Save(session);
        }

        private static string MediaKey(long userId, string mediaGroupId) =>
            $"{userId}:{mediaGroupId}";
    }
}
