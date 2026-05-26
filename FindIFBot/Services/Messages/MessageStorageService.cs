using FindIFBot.Persistence;
using Telegram.Bot.Types;

namespace FindIFBot.Services.Messages
{
    public class MessageStorageService : IMessageStorageService
    {
        private readonly IMessageStore _messages;

        public MessageStorageService(IMessageStore messages)
        {
            _messages = messages;
        }

        public StoredMessage StoreSingle(Message message, string? text, IReadOnlyList<string> photos)
        {
            var stored = new StoredMessage(
                message.Chat.Id,
                message.From?.Id ?? message.Chat.Id,
                text,
                photos,
                message.MediaGroupId,
                message.MessageId
            );

            _messages.Store(message.MessageId, stored);

            return stored;
        }

        public StoredMessage StoreMediaGroup(Message captionMessage, IReadOnlyList<string> photos)
        {
            var stored = new StoredMessage(
                captionMessage.Chat.Id,
                captionMessage.From!.Id,
                captionMessage.Caption,
                photos,
                captionMessage.MediaGroupId,
                captionMessage.MessageId
            );

            _messages.Store(captionMessage.MessageId, stored);

            return stored;
        }
    }
}
