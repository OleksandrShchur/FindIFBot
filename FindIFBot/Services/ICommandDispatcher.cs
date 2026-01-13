using Telegram.Bot.Types;

namespace FindIFBot.Services
{
    public interface ICommandDispatcher
    {
        Task DispatchAsync(Update update);
    }
}
