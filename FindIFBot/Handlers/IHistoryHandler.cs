using Telegram.Bot;
using Telegram.Bot.Types;

namespace FindIFBot.Handlers
{
    public interface IHistoryHandler
    {
        Task HandleAsync(ITelegramBotClient bot, Message message);
    }
}