using FindIFBot.Persistence;
using Telegram.Bot.Types;

namespace FindIFBot.Services.Messages
{
    public interface IMessageStorageService
    {
        StoredMessage StoreSingle(Message message, string? text, IReadOnlyList<string> photos);
        StoredMessage StoreMediaGroup(Message captionMessage, IReadOnlyList<string> photos);
    }
}
