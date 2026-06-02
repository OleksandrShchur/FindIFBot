using Telegram.Bot.Types;

namespace FindIFBot.Services.Messages
{
    public class MediaGroupBuffer : IMediaGroupBuffer
    {
        private readonly Dictionary<string, List<Message>> _buffer = new();
        private readonly object _lock = new();

        public bool Add(long userId, string mediaGroupId, Message message)
        {
            var key = MediaKey(userId, mediaGroupId);

            lock (_lock)
            {
                var isFirst = !_buffer.TryGetValue(key, out var messages);
                if (messages == null)
                {
                    messages = new List<Message>();
                    _buffer[key] = messages;
                }

                messages.Add(message);
                return isFirst;
            }
        }

        public bool TryTake(long userId, string mediaGroupId, out List<Message> messages)
        {
            var key = MediaKey(userId, mediaGroupId);

            lock (_lock)
            {
                if (!_buffer.TryGetValue(key, out var buffered) || buffered.Count == 0)
                {
                    messages = new List<Message>();
                    return false;
                }

                _buffer.Remove(key);
                messages = buffered;
                return true;
            }
        }

        private static string MediaKey(long userId, string mediaGroupId) => $"{userId}:{mediaGroupId}";
    }
}
