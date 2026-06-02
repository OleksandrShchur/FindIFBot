using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using Telegram.Bot.Types;

namespace FindIFBot.Services.Messages
{
    public interface IMediaGroupHandler
    {
        Task BufferAsync(Message message, long userId);
        Task ProcessAsync(List<Message> messages, UserSession session, IUserRequestHistoryRepository history);
    }
}
