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
            if (cb.From.Id != _adminId) return;

            var parts = cb.Data?.Split('|');
            if (parts == null || parts.Length < 3) return;

            var action = parts[0];
            var userId = long.Parse(parts[1]);
            var messageId = int.Parse(parts[2]);

            if (!_messages.TryGet(messageId, out var stored)) return;

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
                    return; // admin message stays

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
            await _bot.SendMessage(
                message.Chat.Id,
                "Очікуйте на публікацію. Триває модерація."
            );

            var keyboard = BuildFindKeyboard(message);

            await _bot.SendMessage(
                _adminId,
                message.Text ?? "(no text)",
                replyMarkup: keyboard
            );
        }

        public async Task SubmitAdAsync(Message message)
        {
            await _bot.SendMessage(
                message.Chat.Id,
                "Матеріал передано адміністраторам."
            );

            var keyboard = BuildAdsKeyboard(message);

            await _bot.SendMessage(
                _adminId,
                message.Text ?? "(no text)",
                replyMarkup: keyboard
            );
        }

        private async Task PublishAsync(long userId, StoredMessage stored)
        {
            var result = await _bot.SendMessage(_outputChannel, stored.Text!);
            await _bot.SendMessage(
                userId,
                $"Опубліковано: {_channelLink}/{result.MessageId}"
            );
        }

        private async Task RejectAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "Запит на публікацію скасовано.",
                replyParameters: new ReplyParameters { MessageId = messageId }
            );
        }

        private async Task DuplicateAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "Схожий запит вже опубліковано.",
                replyParameters: new ReplyParameters { MessageId = messageId }
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

            await _bot.SendMessage(_adminId, stored.Text!, replyMarkup: keyboard);
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
                await _bot.DeleteMessage(
                    cb.Message!.Chat.Id,
                    cb.Message.MessageId
                );
            }
            catch { }
        }

        private InlineKeyboardMarkup BuildFindKeyboard(Message msg) =>
            new(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "Approve",
                        $"+find|{msg.From!.Id}|{msg.MessageId}"
                    ),
                    InlineKeyboardButton.WithCallbackData(
                        "Decline",
                        $"-find|{msg.From.Id}|{msg.MessageId}"
                    )
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "Duplicate",
                        $"?find|{msg.From.Id}|{msg.MessageId}"
                    )
                }
            });

        private InlineKeyboardMarkup BuildAdsKeyboard(Message msg) =>
            new(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "Approve",
                        $"+ads|{msg.From!.Id}|{msg.MessageId}"
                    ),
                    InlineKeyboardButton.WithCallbackData(
                        "Decline",
                        $"-ads|{msg.From.Id}|{msg.MessageId}"
                    )
                }
            });
    }
}
