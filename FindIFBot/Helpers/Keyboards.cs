using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Helpers
{
    public static class Keyboards
    {
        public static ReplyKeyboardMarkup GetKeyboard(bool hasHistory)
        {
            var keyboard = new List<KeyboardButton[]>
            {
                new KeyboardButton[] { "Розпочати пошук" }
            };

            if (hasHistory)
            {
                keyboard.Add(new KeyboardButton[] { "Історія запитів" });
            }

            keyboard.Add(new KeyboardButton[] { "Довідка" });

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }
    }
}
