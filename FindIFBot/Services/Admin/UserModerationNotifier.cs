using FindIFBot.EF.Repositories;
using FindIFBot.Helpers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Services.Admin
{
    public class UserModerationNotifier : IUserModerationNotifier
    {
        private readonly ITelegramBotClient _bot;
        private readonly IUserRequestHistoryRepository _history;

        public UserModerationNotifier(
            ITelegramBotClient bot,
            IUserRequestHistoryRepository history)
        {
            _bot = bot;
            _history = history;
        }

        public async Task NotifySubmittedAsync(long chatId)
        {
            await _bot.SendMessage(
                chatId,
                "⏳ <b>Запит відправлено на модерацію!</b>\n\n" +
                "Очікуйте, будь ласка — наші модератори скоро перевірять ваш допис.\n",
                replyMarkup: Keyboards.GetKeyboard(true),
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
                parseMode: ParseMode.Html
            );
        }

        public async Task NotifyPublishedAsync(long userId, string channelLink)
        {
            await _bot.SendMessage(
                userId,
                "🚀 <b>Готово!</b> Ваш пост уже в каналі!\n\n" +
                $"<a href=\"{channelLink}\">👉 Переглянути публікацію</a>\n\n",
                replyMarkup: Keyboards.GetKeyboard(await _history.HasHistory(userId)),
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
                parseMode: ParseMode.Html
            );
        }

        public async Task NotifyRejectedAsync(long userId, int messageId)
        {
            await _bot.SendMessage(
                userId,
                "❌ <b>Запит відхилено</b>\n\n" +
                "На жаль, наші модератори вирішили не публікувати цей допис.\n" +
                "Це могло статися через невідповідність правилам каналу або інші причини.\n\n" +
                "Не засмучуйся — спробуй ще раз з іншим матеріалом! 🌱\n" +
                "Статус усіх твоїх запитів завжди можна подивитись у /history",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: Keyboards.GetKeyboard(await _history.HasHistory(userId)),
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
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
                "Статус запитів → /history або кнопка «📋 Історія запитів»",
                replyParameters: new ReplyParameters { MessageId = messageId },
                replyMarkup: Keyboards.GetKeyboard(await _history.HasHistory(userId)),
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
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
                replyMarkup: Keyboards.GetKeyboard(await _history.HasHistory(userId)),
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
                parseMode: ParseMode.Html
            );
        }
    }
}
