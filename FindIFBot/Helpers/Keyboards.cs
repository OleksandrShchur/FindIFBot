using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Helpers
{
    public static class Keyboards
    {
        public static ReplyKeyboardMarkup GetKeyboard(bool hasHistory)
        {
            var keyboard = new List<KeyboardButton[]>
            {
                new KeyboardButton[] { "📨 Новий запит" }
            };

            if (hasHistory)
            {
                keyboard.Add(new KeyboardButton[] { "📋 Історія запитів" });
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
