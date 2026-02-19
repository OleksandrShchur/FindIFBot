using Telegram.Bot;
using Telegram.Bot.Types;

namespace FindIFBot.Handlers
{
    public interface IAsyncCommandHandler
    {
        Task HandleAsync(ITelegramBotClient bot, Message message);
    }
}
