using FindIFBot.Persistence;
using Telegram.Bot.Types;

namespace FindIFBot.Services.Messages
{
    public interface IMessageStorageService
    {
        Task<StoredMessage> StoreSingleAsync(Message message, string? text, IReadOnlyList<string> photos);
        Task<StoredMessage> StoreMediaGroupAsync(Message captionMessage, IReadOnlyList<string> photos);
    }
}
