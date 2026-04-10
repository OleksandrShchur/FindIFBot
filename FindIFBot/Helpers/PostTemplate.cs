using FindIFBot.Configuration;

namespace FindIFBot.Helpers
{
    public static class PostTemplate
    {
        public static string Build(string message, TelegramOptions options)
        {
            var botUrl = $"https://t.me/{options.BotUsername.TrimStart('@')}?start";

            return $"{message?.TrimEnd()}\n\n" +
                   $"📢 <a href=\"{options.LinkToChannel}\">Підписка</a> • " +
                   $"💬 <a href=\"{options.ChatInviteLink}\">Чат</a>\n" +
                   $"📨 <a href=\"{botUrl}\">Надіслати свій запит</a>";
        }
    }
}
