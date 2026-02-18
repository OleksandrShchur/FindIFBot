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
                "Привіт, я Telegram бот `Де знайти. Івано-Франківськ`.\n",
                parseMode: ParseMode.Markdown
            );

            var userId = message.From!.Id;
            var hasHistory = await _history.HasHistory(userId);

            var markup = Keyboards.GetKeyboard(hasHistory);

            await bot.SendMessage(
                message.Chat.Id,
                "Оберіть опцію, якою хочете скористатись у нашому боті.",
                replyMarkup: markup
            );
        }
    }
}
