using FindIFBot.Helpers;
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
        private readonly long _adminId;
        private readonly string _outputChannel;
        private readonly string _channelLink;

        public AdminWorkflowService(
            ITelegramBotClient bot,
            IMessageStore messages,
            IAdsPricingService pricing,
            IConfiguration config)
        {
            _bot = bot;
            _messages = messages;
            _pricing = pricing;

            long.TryParse(config["Telegram:AdminId"], out _adminId);
            _outputChannel = config["Telegram:UserOutputChannel"] ?? string.Empty;
            _channelLink = config["Telegram:LinkToChannel"] ?? string.Empty;
        }

        public async Task HandleCallbackAsync(CallbackQuery cb)
        {
            //if (cb.From.Id != _adminId)
            //    return;

            var parts = cb.Data?.Split('|');
            if (parts == null || parts.Length < 3)
                return;

            var action = parts[0];
            var userId = long.Parse(parts[1]);
            var messageId = int.Parse(parts[2]);

            if (!_messages.TryGet(messageId, out var stored))
                return;

            switch (action)
            {
                case "+find":
                    await PublishAsync(userId, stored);
                    break;

                case "-find":
                    await RejectAsync(userId, messageId);
                    break;

                case "?find":
                    await DuplicateAsync(userId, messageId);
                    break;

                case "+ads":
                    await ApproveAdsAsync(userId, messageId, stored);
                    return;

                case "postAds":
                    await PublishAsync(userId, stored);
                    break;

                case "-ads":
                    await RejectAsync(userId, messageId);
                    break;

                case "<money":
                    await InsufficientMoneyAsync(userId, messageId);
                    return;
            }

            await CleanupAsync(cb, messageId);
        }

        public async Task SubmitFindAsync(Message message)
        {
            if (!_messages.TryGet(message.MessageId, out var stored))
                return;

            await _bot.SendMessage(
                message.Chat.Id,
                "Очікуйте на публікацію. Триває модерація.",
                replyMarkup: Keyboards.DefaultMarkup()
            );

            await SendToAdmin(stored, message.MessageId);
        }

        public async Task SubmitAdAsync(Message message)
        {
            if (!_messages.TryGet(message.MessageId, out var stored))
                return;

            await _bot.SendMessage(
                message.Chat.Id,
                "Матеріал передано адміністраторам.",
                replyMarkup: Keyboards.DefaultMarkup()
            );

            var keyboard = BuildAdsKeyboard(message);

            if (stored.Photos.Count > 0)
            {
                var media = stored.Photos
                    .Select((id, i) => new InputMediaPhoto(id)
                    {
                        Caption = i == 0 ? stored.Text : null
                    })
                    .ToArray();

                await _bot.SendMediaGroup(_adminId, media);
                await _bot.SendMessage(_adminId, "Moderation actions:", replyMarkup: keyboard);
            }
            else
            {
                await _bot.SendMessage(_adminId, stored.Text ?? "(no text)", replyMarkup: keyboard);
            }
        }

        private async Task PublishAsync(long userId, StoredMessage stored)
        {
            if (stored.Photos.Count > 0)
            {
                var media = stored.Photos
                    .Select((id, i) => new InputMediaPhoto(id)
                    {
                        Caption = i == 0 ? stored.Text : null
                    })
                    .ToArray();
                var result = await _bot.SendMediaGroup(_outputChannel, media);
                var postId = result.First().MessageId; // Better for album links
                await _bot.SendMessage(
                    userId,
                    $"Ваш пост опубліковано: {_channelLink}/{postId}"
                );
            }
            else
            {
                var result = await _bot.SendMessage(_outputChannel, stored.Text ?? "(no text)");
                await _bot.SendMessage(
                    userId,
                    $"Ваш пост опубліковано: {_channelLink}/{result.MessageId}"
                );
            }
        }

        private async Task RejectAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "Запит на публікацію скасовано.",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: Keyboards.DefaultMarkup()
            );
        }

        private async Task DuplicateAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "Схожий запит вже опубліковано. Скористайтесь пошуком у каналі.",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: Keyboards.DefaultMarkup()
            );
        }

        private async Task ApproveAdsAsync(long userId, int messageId, StoredMessage stored)
        {
            var count = 0;
            try { count = await _bot.GetChatMemberCount(_outputChannel); } catch { }
            var price = _pricing.CalculatePrice(count);
            await _bot.SendMessage(
                userId,
                $"Ціна публікації — {price} грн.",
                replyParameters: new ReplyParameters { MessageId = messageId }
            );
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(
                    $"Post ad ({price})",
                    $"postAds|{userId}|{messageId}") },
                new[] { InlineKeyboardButton.WithCallbackData(
                    "No full sum",
                    $"<money|{userId}|{messageId}") }
            });
            // Fixed message instead of redundant stored.Text
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
        }

        private async Task CleanupAsync(CallbackQuery cb, int messageId)
        {
            _messages.Remove(messageId);

            try
            {
                await _bot.DeleteMessage(cb.Message!.Chat.Id, cb.Message.MessageId);
            }
            catch { }
        }

        private async Task SendToAdmin(StoredMessage stored, int messageId)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "Approve post",
                        $"+find|{stored.UserId}|{messageId}"
                    ),
                    InlineKeyboardButton.WithCallbackData(
                        "Decline post",
                        $"-find|{stored.UserId}|{messageId}"
                    )
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "Duplicated post",
                        $"?find|{stored.UserId}|{messageId}"
                    )
                }
            });

            if (stored.Photos.Count > 0)
            {
                var media = stored.Photos
                    .Select((id, i) => new InputMediaPhoto(id)
                    {
                        Caption = i == 0 ? stored.Text : null
                    })
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
                    InlineKeyboardButton.WithCallbackData(
                        "Approve ads",
                        $"+ads|{msg.From!.Id}|{msg.MessageId}"
                    ),
                    InlineKeyboardButton.WithCallbackData(
                        "Decline ads",
                        $"-ads|{msg.From.Id}|{msg.MessageId}"
                    )
                }
            });
    }
}
