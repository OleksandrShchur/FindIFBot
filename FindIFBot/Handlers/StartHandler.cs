using FindIFBot.Helpers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Handlers
{
    public class StartHandler : IStartHandler
    {
        public async Task HandleAsync(
        ITelegramBotClient bot,
        Message message)
        {
            await bot.SendMessage(
                message.Chat.Id,
                "Привіт, я Telegram бот `Де знайти. Івано-Франківськ`.\n",
                parseMode: ParseMode.Markdown
            );

            await bot.SendMessage(
                message.Chat.Id,
                "Оберіть опцію, якою хочете скористатись у нашому боті.",
                replyMarkup: Keyboards.DefaultMarkup()
            );
        }
    }
}
