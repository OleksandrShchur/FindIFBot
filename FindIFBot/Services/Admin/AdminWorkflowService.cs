using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers;
using FindIFBot.Helpers.Logs;
using FindIFBot.Persistence;
using FindIFBot.Utils;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Services.Admin
{
    public class AdminWorkflowService : IAdminWorkflowService
    {
        private readonly ITelegramBotClient _bot;
        private readonly IMessageStore _messages;
        private readonly IUserSessionRepository _sessions;
        private readonly IUserRequestHistoryRepository _history;
        private readonly IAppLogger<AdminWorkflowService> _logger;
        private readonly TelegramOptions _options;

        private const string Component = "AdminWorkflow";

        public AdminWorkflowService(
            ITelegramBotClient bot,
            IMessageStore messages,
            IUserSessionRepository sessions,
            IUserRequestHistoryRepository history,
            IConfiguration config,
            IAppLogger<AdminWorkflowService> logger,
            IOptions<TelegramOptions> options)
        {
            _bot = bot;
            _messages = messages;
            _sessions = sessions;
            _history = history;
            _logger = logger;
            _options = options.Value;
        }

        public async Task HandleCallbackAsync(CallbackQuery cb)
        {
            var parts = cb.Data?.Split('|');
            if (parts == null || parts.Length < 3)
                return;

            var action = parts[0];
            var userId = long.Parse(parts[1]);
            var messageId = int.Parse(parts[2]);

            await _logger.LogInfo(Component, $"Callback received | Action: {action} | UserId: {userId} | MessageId: {messageId} | FromUserId: {cb.From.Id}");

            // Sender validation
            if (action == "proceed" || action == "cancel")
            {
                if (cb.From.Id != userId)
                {
                    await _logger.LogWarning(Component, $"Invalid sender for user callback | Expected: {userId} | Actual: {cb.From.Id}");
                    
                    return;
                }
            }
            else
            {
                if (cb.From.Id != _options.AdminId)
                {
                    await _logger.LogWarning(Component, $"Invalid sender for admin callback | Expected: {_options.AdminId} | Actual: {cb.From.Id}");
                    
                    return;
                }
            }

            if (!_messages.TryGet(messageId, out var stored))
            {
                await _logger.LogError(Component, $"Stored message not found on callback | UserId: {userId} | MessageId: {messageId}");
                
                return;
            }

            await _bot.AnswerCallbackQuery(cb.Id);

            switch (action)
            {
                case "+find":
                    await _logger.LogInfo(Component, $"Admin approved find request | UserId: {userId} | MessageId: {messageId}");
                    await PublishAsync(userId, stored);
                    break;
                case "-find":
                    await _logger.LogInfo(Component, $"Admin rejected find request | UserId: {userId} | MessageId: {messageId}");
                    await RejectAsync(userId, messageId);
                    break;
                case "?find":
                    await _logger.LogInfo(Component, $"Admin marked find as duplicate | UserId: {userId} | MessageId: {messageId}");
                    await DuplicateAsync(userId, messageId);
                    break;
                case "proceed":
                    await _logger.LogInfo(Component, $"User confirmed submission | UserId: {userId} | MessageId: {messageId}");
                    await SubmitAskAsync(stored);
                    var session = _sessions.Get(userId);
                    session.State = UserState.Idle;
                    _sessions.Save(session);

                    // Delete only the confirmation message (inline keyboard) in user's chat
                    // Do NOT remove from _messages store – it must remain for admin moderation
                    try
                    {
                        await _bot.DeleteMessage(cb.Message!.Chat.Id, cb.Message.MessageId);
                    }
                    catch { }

                    return;
                case "cancel":
                    await _logger.LogInfo(Component, $"User cancelled submission | UserId: {userId} | MessageId: {messageId}");
                    await CancelFindAsync(userId, messageId);
                    await CleanupAsync(cb, messageId);

                    return;
            }

            await CleanupAsync(cb, messageId);
        }

        public async Task SubmitAskAsync(StoredMessage stored)
        {
            await _logger.LogInfo(Component,
                $"Submitting find request to moderation | UserId: {stored.UserId} | MessageId: {stored.MessageId} | Photos: {stored.Photos.Count}");

            await _bot.SendMessage(
                stored.ChatId,
                "Очікуйте на публікацію. Триває модерація.",
                replyMarkup: Keyboards.GetKeyboard(true)
            );

            var request = new UserRequest
            {
                Id = Guid.NewGuid(),
                UserId = stored.UserId,
                Status = RequestStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                UserMessageId = stored.MessageId
            };

            await _history.Add(request);

            await SendToAdmin(stored, stored.MessageId);
        }

        private async Task PublishAsync(long userId, StoredMessage stored)
        {
            var channelLink = string.Empty;
            var postId = 0;
            var postText = string.Empty;

            if (stored.Photos.Count > 0)
            {
                var media = stored.Photos
                    .Select((id, i) => new InputMediaPhoto(id) { Caption = i == 0 ? stored.Text : null })
                    .ToArray();
                var result = await _bot.SendMediaGroup(_options.UserOutputChannel, media);
                postText = TextUtils.GetTextPreview(result.First().Caption);
                postId = result.First().MessageId;
            }
            else
            {
                var result = await _bot.SendMessage(_options.UserOutputChannel, stored.Text ?? "(no text)");
                postText = TextUtils.GetTextPreview(result.Text);
                postId = result.MessageId;
            }

            channelLink = $"{_options.LinkToChannel}/{postId}";

            await _bot.SendMessage(
                userId,
                $"Ваш пост опубліковано: <a href=\"{channelLink}\">{postText}</a>",
                replyMarkup: Keyboards.GetKeyboard(await _history.HasHistory(userId)),
                parseMode: ParseMode.Html
            );

            await _logger.LogInfo(Component,
                $"Request published | UserId: {userId} | MessageId: {stored.MessageId} | ChannelLink: {channelLink} | Photos: {stored.Photos.Count}");

            var requests = await _history.GetByUserId(userId);
            var request = requests.FirstOrDefault(r => r.UserMessageId == stored.MessageId && r.Status == RequestStatus.Pending);
            if (request != null)
            {
                request.Status = RequestStatus.Approved;
                request.ChannelLink = channelLink;
                await _history.Update(request);

                await _logger.LogInfo(Component, $"History updated to APPROVED | UserId: {userId} | MessageId: {stored.MessageId}");
            }
        }

        private async Task RejectAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "Запит на публікацію відхилено.",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: Keyboards.GetKeyboard(await _history.HasHistory(userId))
            );

            await _logger.LogInfo(Component, $"Request rejected | UserId: {userId} | MessageId: {messageId}");

            var requests = await _history.GetByUserId(userId);
            var request = requests.FirstOrDefault(r => r.UserMessageId == messageId && r.Status == RequestStatus.Pending);
            if (request != null)
            {
                request.Status = RequestStatus.Rejected;
                await _history.Update(request);

                await _logger.LogInfo(Component, $"History updated to REJECTED | UserId: {userId} | MessageId: {messageId}");
            }
        }

        private async Task DuplicateAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "Схожий запит вже опубліковано. Скористайтесь пошуком у каналі.",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: Keyboards.GetKeyboard(await _history.HasHistory(userId))
            );

            await _logger.LogInfo(Component, $"Request marked as duplicate | UserId: {userId} | MessageId: {messageId}");

            var requests = await _history.GetByUserId(userId);
            var request = requests.FirstOrDefault(r => r.UserMessageId == messageId && r.Status == RequestStatus.Pending);
            if (request != null)
            {
                request.Status = RequestStatus.Duplicate;
                await _history.Update(request);

                await _logger.LogInfo(Component, "History updated to DUPLICATE | UserId: {userId} | MessageId: {messageId}");
            }
        }

        private async Task CleanupAsync(CallbackQuery cb, int messageId)
        {
            _messages.Remove(messageId);
            await _logger.LogInfo(Component, $"Cleanup: removed stored message | MessageId: {messageId}");

            try
            {
                await _bot.DeleteMessage(cb.Message!.Chat.Id, cb.Message.MessageId);
            }
            catch { }
        }

        private async Task SendToAdmin(StoredMessage stored, int messageId)
        {
            await _logger.LogInfo(Component,
                $"Sending find request to admin | UserId: {stored.UserId} | MessageId: {messageId} | Photos: {stored.Photos.Count}");

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Approve post", $"+find|{stored.UserId}|{messageId}"),
                    InlineKeyboardButton.WithCallbackData("Decline post", $"-find|{stored.UserId}|{messageId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Duplicated post", $"?find|{stored.UserId}|{messageId}")
                }
            });

            if (stored.Photos.Count > 0)
            {
                var media = stored.Photos
                    .Select((id, i) => new InputMediaPhoto(id) { Caption = i == 0 ? stored.Text : null })
                    .ToArray();
                await _bot.SendMediaGroup(_options.AdminId, media);
                await _bot.SendMessage(_options.AdminId, "Moderation actions:", replyMarkup: keyboard);
            }
            else
            {
                await _bot.SendMessage(_options.AdminId, stored.Text ?? "(no text)", replyMarkup: keyboard);
            }
        }

        private async Task CancelFindAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "Публікацію скасовано.",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: Keyboards.GetKeyboard(await _history.HasHistory(userId))
            );

            await _logger.LogInfo(Component, $"User cancelled find request | UserId: {userId} | MessageId: {messageId}");

            var session = _sessions.Get(userId);
            session.State = UserState.Idle;
            _sessions.Save(session);
        }
    }
}
