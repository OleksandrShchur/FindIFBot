using FindIFBot.EF.Repositories;
using FindIFBot.Helpers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Handlers
{
    public class StartHandler : IAsyncCommandHandler
    {
        private readonly IUserRequestHistoryRepository _history;

        public StartHandler(IUserRequestHistoryRepository history)
        {
            _history = history;
        }

        public async Task HandleAsync(
            ITelegramBotClient bot,
            Message message)
        {
            await bot.SendMessage(
                message.Chat.Id,
                "👋 <b>Привіт!</b>\n\n" +
                "Мене звати <b>Франківськ Питає Бот</b> 🤖\n\n" +
                "Я допомагаю надсилати запити на публікацію в канал.\n" +
                "Радий тебе бачити! ❤️",
                parseMode: ParseMode.Html
            );

            var userId = message.From!.Id;
            var hasHistory = await _history.HasHistory(userId);
            var markup = Keyboards.GetKeyboard(hasHistory);

            await bot.SendMessage(
                message.Chat.Id,
                "🛠 <b>Оберіть опцію, якою хочете скористатися:</b>",
                replyMarkup: markup,
                parseMode: ParseMode.Html
            );
        }
    }
}
