using FindIFBot.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Handlers
{
    public class AdsCollaborationHandler : IAsyncCommandHandler
    {
        private readonly TelegramOptions _options;
        private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };

        public AdsCollaborationHandler(IOptions<TelegramOptions> options)
        {
            _options = options.Value;
        }

        public async Task HandleAsync(ITelegramBotClient bot, Message message)
        {
            var keyboard = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithUrl("✍️ Написати в дірект", _options.DirectChatLink));

            await bot.SendMessage(
                message.Chat.Id,
                BuildPolicy(),
                replyMarkup: keyboard,
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html
            );
        }

        private static string BuildPolicy() =>
            "🤝 <b>Реклама та співпраця в каналі «Франківськ Питає»</b>\n\n" +

            "Хочете розповісти про свій бізнес, послугу чи подію нашій спільноті Івано-Франківська? " +
            "Ми відкриті до реклами та партнерства ❤️\n\n" +

            "✅ <b>Що ми розміщуємо:</b>\n" +
            "• Локальні бізнеси, послуги та заклади міста\n" +
            "• Анонси подій, акцій та відкриттів\n" +
            "• Партнерські та інформаційні колаборації\n" +
            "• Корисні пропозиції для мешканців Франківська\n\n" +

            "📝 <b>Як підготувати рекламний пост:</b>\n" +
            "• Чіткий заголовок і суть пропозиції в перших рядках\n" +
            "• Деталі: що пропонуєте, ціна/умови, локація, дата (якщо є)\n" +
            "• Контакт для звʼязку: @username, телефон або сайт\n" +
            "• 1–5 (10 максимум) якісних фото за потреби\n\n" +

            "❌ <b>Чого не розміщуємо:</b>\n" +
            "• Політику, агітацію, релігійні суперечки\n" +
            "• Азартні ігри, сумнівні «легкі заробітки», фінансові піраміди\n" +
            "• Контент 18+, шкідливі чи заборонені товари й послуги\n" +
            "• Неправдиву чи оманливу рекламу\n\n" +

            "💬 <b>Вартість і умови</b> обговорюються індивідуально в приватному чаті — " +
            "напишіть нам, і ми підберемо зручний формат розміщення.\n\n" +

            "👇 Натисніть кнопку нижче, щоб написати нам у дірект:";
    }
}
