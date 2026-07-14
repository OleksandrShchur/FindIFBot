using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Helpers
{
    public static class Keyboards
    {
        public const string AdminPendingCaption = "⏳ Черга модерації";

        public static ReplyKeyboardMarkup GetKeyboard(bool hasHistory, bool isAdmin = false)
        {
            var keyboard = new List<KeyboardButton[]>
            {
                new KeyboardButton[] { "📨 Надіслати запит" },
                new KeyboardButton[] { "🤝 Реклама та співпраця" }
            };

            if (hasHistory)
            {
                keyboard.Add(new KeyboardButton[] { "📋 Історія запитів" });
            }

            if (isAdmin)
            {
                keyboard.Add(new KeyboardButton[] { AdminPendingCaption });
            }

            keyboard.Add(new KeyboardButton[]
            {
                "ℹ️ Довідка",
                "📜 Правила"
            });

            keyboard.Add(new KeyboardButton[] 
            { 
                "❤️ Підтримати",
                "🔗 Канал"
            });

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }
    }
}
