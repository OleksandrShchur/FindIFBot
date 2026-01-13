using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Helpers
{
    public static class Keyboards
    {
        public static ReplyKeyboardMarkup DefaultMarkup()
        {
            return new ReplyKeyboardMarkup(new[]
            {
            new KeyboardButton[] { "Розпочати пошук", "Розмістити рекламу" },
            new KeyboardButton[] { "Правила розміщення реклами" },
            new KeyboardButton[] { "Підтримати нас", "Довідка" },
            new KeyboardButton[] { "Запропонувати покращення" }
        })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }
    }
}
