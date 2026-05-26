using Telegram.Bot.Types;

namespace FindIFBot.Services.Messages
{
    public interface IMessageDispatchService
    {
        Task HandleAsync(Message message);
    }
}
