using Telegram.Bot;
using Telegram.Bot.Types;

namespace FindIFBot.Handlers
{
    public interface IStartHandler
    {
        Task HandleAsync(ITelegramBotClient bot, Message message);
    }
}
