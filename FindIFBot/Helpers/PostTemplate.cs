using FindIFBot.Configuration;

namespace FindIFBot.Helpers
{
    public static class PostTemplate
    {
        public static string Build(string? message, TelegramOptions options)
        {
            var channelLink = ToChannelDeepLink(options.LinkToChannel);
            var chatLink = ToInviteDeepLink(options.ChatInviteLink);
            var botLink = $"tg://resolve?domain={options.BotUsername.TrimStart('@')}&start=";

            return $"{message?.TrimEnd()}\n\n" +
                   $"📢 <a href=\"{channelLink}\">Підписка</a> • " +
                   $"💬 <a href=\"{chatLink}\">Чат</a>\n" +
                   $"📨 <a href=\"{botLink}\">Надіслати свій запит</a>";
        }

        private static string ToChannelDeepLink(string? link)
        {
            var domain = ExtractDomain(link);
            return string.IsNullOrEmpty(domain) ? link ?? string.Empty : $"tg://resolve?domain={domain}";
        }

        private static string ToInviteDeepLink(string? link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return string.Empty;
            }

            var invite = link.Trim();
            var plusIndex = invite.IndexOf('+');
            if (plusIndex >= 0)
            {
                return $"tg://join?invite={invite[(plusIndex + 1)..]}";
            }

            const string joinChat = "/joinchat/";
            var joinIndex = invite.IndexOf(joinChat, StringComparison.OrdinalIgnoreCase);
            if (joinIndex >= 0)
            {
                return $"tg://join?invite={invite[(joinIndex + joinChat.Length)..]}";
            }

            return invite;
        }

        private static string ExtractDomain(string? link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return string.Empty;
            }

            var value = link.Trim();

            if (value.StartsWith('@'))
            {
                return value.TrimStart('@');
            }

            const string tMe = "t.me/";
            var index = value.IndexOf(tMe, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                value = value[(index + tMe.Length)..];
            }

            return value.Trim('/');
        }
    }
}
