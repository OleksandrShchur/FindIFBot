using Telegram.Bot.Types;

namespace FindIFBot.Services.Messages
{
    public interface IMessageCommandRouter
    {
        Task RouteAsync(Message message, string normalized);
    }
}
