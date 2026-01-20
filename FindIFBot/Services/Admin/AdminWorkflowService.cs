using FindIFBot.Domain;
using FindIFBot.Helpers;
using FindIFBot.Helpers.Logs;
using FindIFBot.Persistence;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Services.Admin
{
    public class AdminWorkflowService : IAdminWorkflowService
    {
        private readonly ITelegramBotClient _bot;
        private readonly IMessageStore _messages;
        private readonly IAdsPricingService _pricing;
        private readonly IUserSessionRepository _sessions;
        private readonly IUserRequestHistoryRepository _history;
        private readonly IAppLogger _logger;

        private readonly long _adminId;
        private readonly string _outputChannel;
        private readonly string _channelLink;

        private const string Component = "AdminWorkflow";

        public AdminWorkflowService(
            ITelegramBotClient bot,
            IMessageStore messages,
            IAdsPricingService pricing,
            IUserSessionRepository sessions,
            IUserRequestHistoryRepository history,
            IConfiguration config,
            IAppLogger logger)
        {
            _bot = bot;
            _messages = messages;
            _pricing = pricing;
            _sessions = sessions;
            _history = history;
            _logger = logger;

            long.TryParse(config["Telegram:AdminId"], out _adminId);
            _outputChannel = config["Telegram:UserOutputChannel"] ?? string.Empty;
            _channelLink = config["Telegram:LinkToChannel"] ?? string.Empty;
        }

        public async Task HandleCallbackAsync(CallbackQuery cb)
        {
            var parts = cb.Data?.Split('|');
            if (parts == null || parts.Length < 3)
                return;

            var action = parts[0];
            var userId = long.Parse(parts[1]);
            var messageId = int.Parse(parts[2]);

            _logger.Log(Component, LogType.Info,
                $"Callback received | Action: {action} | UserId: {userId} | MessageId: {messageId} | FromUserId: {cb.From.Id}");

            // Sender validation
            if (action == "proceed" || action == "cancel")
            {
                if (cb.From.Id != userId)
                {
                    _logger.Log(Component, LogType.Warning,
                        $"Invalid sender for user callback | Expected: {userId} | Actual: {cb.From.Id}");
                    return;
                }
            }
            else
            {
                if (cb.From.Id != _adminId)
                {
                    _logger.Log(Component, LogType.Warning,
                        $"Invalid sender for admin callback | Expected: {_adminId} | Actual: {cb.From.Id}");
                    return;
                }
            }

            if (!_messages.TryGet(messageId, out var stored))
            {
                _logger.Log(Component, LogType.Error,
                    $"Stored message not found on callback | UserId: {userId} | MessageId: {messageId}");
                return;
            }

            await _bot.AnswerCallbackQuery(cb.Id);

            switch (action)
            {
                case "+find":
                    _logger.Log(Component, LogType.Info, $"Admin approved find request | UserId: {userId} | MessageId: {messageId}");
                    await PublishAsync(userId, stored);
                    break;
                case "-find":
                    _logger.Log(Component, LogType.Info, $"Admin rejected find request | UserId: {userId} | MessageId: {messageId}");
                    await RejectAsync(userId, messageId);
                    break;
                case "?find":
                    _logger.Log(Component, LogType.Info, $"Admin marked find as duplicate | UserId: {userId} | MessageId: {messageId}");
                    await DuplicateAsync(userId, messageId);
                    break;
                case "+ads":
                    _logger.Log(Component, LogType.Info, $"Admin approved ads | UserId: {userId} | MessageId: {messageId}");
                    await ApproveAdsAsync(userId, messageId, stored);
                    return;
                case "postAds":
                    _logger.Log(Component, LogType.Info, $"Admin published ads after payment | UserId: {userId} | MessageId: {messageId}");
                    await PublishAsync(userId, stored);
                    break;
                case "-ads":
                    _logger.Log(Component, LogType.Info, $"Admin rejected ads | UserId: {userId} | MessageId: {messageId}");
                    await RejectAsync(userId, messageId);
                    break;
                case "<money":
                    _logger.Log(Component, LogType.Info, $"Insufficient payment for ads | UserId: {userId} | MessageId: {messageId}");
                    await InsufficientMoneyAsync(userId, messageId);
                    return;
                case "proceed":
                    _logger.Log(Component, LogType.Info, $"User confirmed submission | UserId: {userId} | MessageId: {messageId}");
                    await SubmitFindAsync(stored);
                    var session = _sessions.Get(userId);
                    session.State = UserState.Idle;
                    _sessions.Save(session);
                    await CleanupAsync(cb, messageId);
                    return;
                case "cancel":
                    _logger.Log(Component, LogType.Info, $"User cancelled submission | UserId: {userId} | MessageId: {messageId}");
                    await CancelFindAsync(userId, messageId);
                    await CleanupAsync(cb, messageId);
                    return;
            }

            await CleanupAsync(cb, messageId);
        }

        public async Task SubmitFindAsync(StoredMessage stored)
        {
            _logger.Log(Component, LogType.Info,
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
                StoredMessage = stored,
                Status = RequestStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                UserMessageId = stored.MessageId
            };

            _history.Add(request);
            _messages.Store(stored.MessageId, stored);

            await SendToAdmin(stored, stored.MessageId);
        }

        public async Task SubmitAdAsync(Message message)
        {
            if (!_messages.TryGet(message.MessageId, out var stored))
            {
                _logger.Log(Component, LogType.Error,
                    $"Stored message not found on ad submission | MessageId: {message.MessageId}");
                return;
            }

            _logger.Log(Component, LogType.Info,
                $"Ad submitted by user | UserId: {stored.UserId} | MessageId: {stored.MessageId} | Photos: {stored.Photos.Count}");

            await _bot.SendMessage(
                message.Chat.Id,
                "Матеріал передано адміністраторам.",
                replyMarkup: Keyboards.GetKeyboard(true)
            );

            var request = new UserRequest
            {
                Id = Guid.NewGuid(),
                UserId = stored.UserId,
                StoredMessage = stored,
                Status = RequestStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                UserMessageId = stored.MessageId
            };

            _history.Add(request);

            var keyboard = BuildAdsKeyboard(message);

            if (stored.Photos.Count > 0)
            {
                var media = stored.Photos
                    .Select((id, i) => new InputMediaPhoto(id) { Caption = i == 0 ? stored.Text : null })
                    .ToArray();
                await _bot.SendMediaGroup(_adminId, media);
                await _bot.SendMessage(_adminId, "Moderation actions:", replyMarkup: keyboard);
            }
            else
            {
                await _bot.SendMessage(_adminId, stored.Text ?? "(no text)", replyMarkup: keyboard);
            }

            _logger.Log(Component, LogType.Info,
                $"Ad forwarded to admin for moderation | UserId: {stored.UserId} | MessageId: {stored.MessageId}");
        }

        private async Task PublishAsync(long userId, StoredMessage stored)
        {
            string channelLink = "";
            int postId = 0;

            if (stored.Photos.Count > 0)
            {
                var media = stored.Photos
                    .Select((id, i) => new InputMediaPhoto(id) { Caption = i == 0 ? stored.Text : null })
                    .ToArray();
                var result = await _bot.SendMediaGroup(_outputChannel, media);
                postId = result.First().MessageId;
            }
            else
            {
                var result = await _bot.SendMessage(_outputChannel, stored.Text ?? "(no text)");
                postId = result.MessageId;
            }

            channelLink = $"{_channelLink}/{postId}";

            await _bot.SendMessage(
                userId,
                $"Ваш пост опубліковано: {channelLink}",
                replyMarkup: Keyboards.GetKeyboard(_history.GetByUserId(userId).Any())
            );

            _logger.Log(Component, LogType.Info,
                $"Request published | UserId: {userId} | MessageId: {stored.MessageId} | ChannelLink: {channelLink} | Photos: {stored.Photos.Count}");

            var requests = _history.GetByUserId(userId);
            var request = requests.FirstOrDefault(r => r.UserMessageId == stored.MessageId && r.Status == RequestStatus.Pending);
            if (request != null)
            {
                request.Status = RequestStatus.Approved;
                request.ChannelLink = channelLink;
                _history.Update(request);

                _logger.Log(Component, LogType.Info,
                    $"History updated to APPROVED | UserId: {userId} | MessageId: {stored.MessageId}");
            }
        }

        private async Task RejectAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "Запит на публікацію відхилено.",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: Keyboards.GetKeyboard(_history.GetByUserId(userId).Any())
            );

            _logger.Log(Component, LogType.Info, $"Request rejected | UserId: {userId} | MessageId: {messageId}");

            var requests = _history.GetByUserId(userId);
            var request = requests.FirstOrDefault(r => r.UserMessageId == messageId && r.Status == RequestStatus.Pending);
            if (request != null)
            {
                request.Status = RequestStatus.Rejected;
                _history.Update(request);

                _logger.Log(Component, LogType.Info,
                    $"History updated to REJECTED | UserId: {userId} | MessageId: {messageId}");
            }
        }

        private async Task DuplicateAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "Схожий запит вже опубліковано. Скористайтесь пошуком у каналі.",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: Keyboards.GetKeyboard(_history.GetByUserId(userId).Any())
            );

            _logger.Log(Component, LogType.Info, $"Request marked as duplicate | UserId: {userId} | MessageId: {messageId}");

            var requests = _history.GetByUserId(userId);
            var request = requests.FirstOrDefault(r => r.UserMessageId == messageId && r.Status == RequestStatus.Pending);
            if (request != null)
            {
                request.Status = RequestStatus.Duplicate;
                _history.Update(request);

                _logger.Log(Component, LogType.Info,
                    $"History updated to DUPLICATE | UserId: {userId} | MessageId: {messageId}");
            }
        }

        private async Task ApproveAdsAsync(long userId, int messageId, StoredMessage stored)
        {
            var count = 0;
            try { count = await _bot.GetChatMemberCount(_outputChannel); } catch { }
            var price = _pricing.CalculatePrice(count);

            _logger.Log(Component, LogType.Info,
                $"Ads approved, price calculated | UserId: {userId} | MessageId: {messageId} | Price: {price} грн");

            await _bot.SendMessage(
                userId,
                $"Ціна публікації — {price} грн.",
                replyParameters: new ReplyParameters { MessageId = messageId }
            );

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData($"Post ad ({price})", $"postAds|{userId}|{messageId}") },
                new[] { InlineKeyboardButton.WithCallbackData("No full sum", $"<money|{userId}|{messageId}") }
            });

            await _bot.SendMessage(
                _adminId,
                $"Реклама схвалена. Ціна: {price} грн. Очікуємо оплату для публікації.",
                replyMarkup: keyboard
            );
        }

        private async Task InsufficientMoneyAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "Сума оплати некоректна.",
                replyParameters: new ReplyParameters { MessageId = messageId }
            );

            _logger.Log(Component, LogType.Info, $"Insufficient payment reported | UserId: {userId} | MessageId: {messageId}");
        }

        private async Task CleanupAsync(CallbackQuery cb, int messageId)
        {
            _messages.Remove(messageId);
            _logger.Log(Component, LogType.Info, $"Cleanup: removed stored message | MessageId: {messageId}");

            try
            {
                await _bot.DeleteMessage(cb.Message!.Chat.Id, cb.Message.MessageId);
            }
            catch { }
        }

        private async Task SendToAdmin(StoredMessage stored, int messageId)
        {
            _logger.Log(Component, LogType.Info,
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
                await _bot.SendMediaGroup(_adminId, media);
                await _bot.SendMessage(_adminId, "Moderation actions:", replyMarkup: keyboard);
            }
            else
            {
                await _bot.SendMessage(_adminId, stored.Text ?? "(no text)", replyMarkup: keyboard);
            }
        }

        private InlineKeyboardMarkup BuildAdsKeyboard(Message msg) =>
            new(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Approve ads", $"+ads|{msg.From!.Id}|{msg.MessageId}"),
                    InlineKeyboardButton.WithCallbackData("Decline ads", $"-ads|{msg.From!.Id}|{msg.MessageId}")
                }
            });

        private async Task CancelFindAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "Публікацію скасовано.",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: Keyboards.GetKeyboard(_history.GetByUserId(userId).Any())
            );

            _logger.Log(Component, LogType.Info, $"User cancelled find request | UserId: {userId} | MessageId: {messageId}");

            var session = _sessions.Get(userId);
            session.State = UserState.Idle;
            _sessions.Save(session);
        }
    }
}
