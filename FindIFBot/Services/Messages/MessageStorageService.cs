using FindIFBot.Persistence;
using FindIFBot.Utils;
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

        public async Task<StoredMessage> StoreSingleAsync(Message message, string? text, IReadOnlyList<string> photos)
        {
            var entities = message.Text != null
                ? MessageEntityHtml.FromTelegram(message.Entities)
                : MessageEntityHtml.FromTelegram(message.CaptionEntities);

            var stored = new StoredMessage(
                message.Chat.Id,
                message.From?.Id ?? message.Chat.Id,
                text,
                photos,
                message.MediaGroupId,
                message.MessageId,
                entities
            );

            await _messages.StoreAsync(stored);

            return stored;
        }

        public async Task<StoredMessage> StoreMediaGroupAsync(Message captionMessage, IReadOnlyList<string> photos)
        {
            var stored = new StoredMessage(
                captionMessage.Chat.Id,
                captionMessage.From!.Id,
                captionMessage.Caption,
                photos,
                captionMessage.MediaGroupId,
                captionMessage.MessageId,
                MessageEntityHtml.FromTelegram(captionMessage.CaptionEntities)
            );

            await _messages.StoreAsync(stored);

            return stored;
        }
    }
}
