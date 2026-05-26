using Telegram.Bot.Types;

namespace FindIFBot.Services.Messages
{
    public interface IMediaGroupBuffer
    {
        bool Add(long userId, string mediaGroupId, Message message);
        bool TryTake(long userId, string mediaGroupId, out List<Message> messages);
    }
}
