using FindIFBot.Configuration;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Services.Admin
{
    public class UserModerationNotifier : IUserModerationNotifier
    {
        private readonly ITelegramBotClient _bot;
        private readonly IUserRequestHistoryRepository _history;
        private readonly TelegramOptions _options;
        private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };

        public UserModerationNotifier(
            ITelegramBotClient bot,
            IUserRequestHistoryRepository history,
            IOptions<TelegramOptions> options)
        {
            _bot = bot;
            _history = history;
            _options = options.Value;
        }

        public async Task NotifySubmittedAsync(long chatId, int requestId)
        {
            await _bot.SendMessage(
                chatId,
                "⏳ <b>Запит відправлено на модерацію!</b>\n\n" +
                "Очікуйте, будь ласка — наші модератори скоро перевірять ваш допис.\n\n" +
                $"🆔 <b>ID запиту:</b> #<code>{requestId}</code>",
                replyMarkup: Keyboards.GetKeyboard(true, chatId == _options.AdminId),
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html
            );
        }

        public async Task NotifyPublishedAsync(long userId, string channelLink, int requestId)
        {
            await _bot.SendMessage(
                userId,
                $"🚀 <b>Готово!</b> Ваш запит <code>#{requestId}</code> опубліковано в каналі!\n\n" +
                $"<a href=\"{channelLink}\">👉 Переглянути публікацію</a>\n\n",
                replyMarkup: await KeyboardForAsync(userId),
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html
            );
        }

        public async Task NotifyRejectedAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                $"❌ <b>Запит <code>#{messageId}</code> відхилено</b>\n\n" +
                "На жаль, наші модератори вирішили не публікувати цей допис.\n" +
                "Це могло статися через невідповідність правилам каналу або інші причини.\n\n" +
                "Не засмучуйся — спробуй ще раз з іншим матеріалом! 🌱\n" +
                "Статус усіх твоїх запитів завжди можна подивитись у /history\n\n", 
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: await KeyboardForAsync(userId),
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html
            );
        }

        public async Task NotifyDuplicateAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "🔍 <b>Схожий допис уже є в каналі</b>\n\n" +
                "На жаль, ми вже публікували дуже подібний запит раніше.\n" +
                "Щоб не дублювати контент, перевір, будь ласка, пошук у каналі — можливо, відповідь уже там 🌟\n\n" +
                "Якщо хочеш надіслати щось нове чи по-іншому — пиши, з радістю розглянемо! 🚀\n" +
                "Статус запитів → /history або кнопка «📋 Історія запитів»\n\n" +
                $"🆔 <b>ID запиту:</b> #<code>{messageId}</code>",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: await KeyboardForAsync(userId),
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html
            );
        }

        public async Task NotifyAdvertisementAsync(long userId, int messageId)
        {
            var keyboard = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithUrl("✍️ Написати в дірект", _options.DirectChatLink));

            await _bot.SendMessage(
                userId,
                "📣 <b>Ваш запит схожий на рекламу</b>\n\n" +
                "Дякуємо за допис! Схоже, він просуває ваш бізнес, послугу чи подію — " +
                "а такі публікації ми розміщуємо на умовах реклами та співпраці. 🤝\n\n" +
                "Це не відмова — ми з радістю розкажемо про вас нашій спільноті Івано-Франківська. " +
                "Формат, вартість та умови розміщення ми узгоджуємо індивідуально в приватному чаті.\n\n" +
                "👇 Натисніть кнопку нижче, щоб написати нам у дірект — і ми все організуємо:\n\n" +
                $"🆔 <b>ID запиту:</b> #<code>{messageId}</code>",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: keyboard,
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html
            );
        }

        public async Task NotifyNeedsAttentionAsync(long userId, int messageId)
        {
            var keyboard = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithUrl("✍️ Написати в дірект", _options.DirectChatLink));

            await _bot.SendMessage(
                userId,
                "💬 <b>Модератор має уточнення</b>\n\n" +
                "Ми переглянули ваш запит і хочемо коротко уточнити кілька деталей перед публікацією.\n\n" +
                "Напишіть нам у дірект — так швидше все узгодимо 🙌\n\n" +
                "👇 Натисніть кнопку нижче:\n\n" +
                $"🆔 <b>ID запиту:</b> #<code>{messageId}</code>",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: keyboard,
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html
            );
        }

        public async Task NotifyCancelledAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "❌ <b>Публікацію скасовано</b>\n\n" +
                "Ваш запит було успішно скасовано.\n" +
                "Нічого не було надіслано на модерацію і нічого не опубліковано.\n\n" +
                "Якщо передумали або хочете надіслати щось інше — просто почніть новий запит!\n" +
                "Переглянути історію запитів: /history або кнопка «📋 Історія запитів» нижче",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: await KeyboardForAsync(userId),
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html
            );
        }

        private async Task<ReplyKeyboardMarkup> KeyboardForAsync(long userId) =>
            Keyboards.GetKeyboard(
                await _history.HasHistory(userId),
                userId == _options.AdminId);
    }
}
