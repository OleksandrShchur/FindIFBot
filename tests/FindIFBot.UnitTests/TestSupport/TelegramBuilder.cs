using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.UnitTests.TestSupport
{
    /// <summary>
    /// Minimal builders for the Telegram update shapes the bot actually consumes.
    /// Only the fields the production code reads are populated.
    /// </summary>
    internal static class TelegramBuilder
    {
        public static User User(long id = 100, bool isBot = false) =>
            new() { Id = id, IsBot = isBot, FirstName = "Test" };

        public static Chat Chat(long id = 100) =>
            new() { Id = id, Type = ChatType.Private };

        public static Message TextMessage(
            string? text = "hello",
            long userId = 100,
            long chatId = 100,
            int messageId = 1,
            string? caption = null,
            string? mediaGroupId = null,
            PhotoSize[]? photo = null) =>
            new()
            {
                Id = messageId,
                From = User(userId),
                Chat = Chat(chatId),
                Text = text,
                Caption = caption,
                MediaGroupId = mediaGroupId,
                Photo = photo,
                Date = DateTime.UtcNow
            };

        public static CallbackQuery CallbackQuery(string data, long userId = 100, long chatId = 100) =>
            new()
            {
                Id = "cb-1",
                Data = data,
                From = User(userId),
                Message = TextMessage(text: null, userId: userId, chatId: chatId),
                ChatInstance = "chat-instance"
            };

        public static Update MessageUpdate(Message message, int id = 1) =>
            new() { Id = id, Message = message };

        public static Update CallbackUpdate(CallbackQuery callback, int id = 1) =>
            new() { Id = id, CallbackQuery = callback };

        public static Update EmptyUpdate(int id = 1) =>
            new() { Id = id };
    }
}
