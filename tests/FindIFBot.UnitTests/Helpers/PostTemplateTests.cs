using FindIFBot.Configuration;
using FindIFBot.Helpers;

namespace FindIFBot.UnitTests.Helpers
{
    public class PostTemplateTests
    {
        [Fact]
        public void Build_UsesTelegramDeepLinksInsteadOfHttps()
        {
            var options = new TelegramOptions
            {
                LinkToChannel = "https://t.me/ask_frankivsk",
                ChatInviteLink = "https://t.me/+YAgDZDhECi00M2Yy",
                BotUsername = "ask_if_bot"
            };

            var result = PostTemplate.Build("Hello", options);

            result.Should().Contain("tg://resolve?domain=ask_frankivsk");
            result.Should().Contain("tg://join?invite=YAgDZDhECi00M2Yy");
            result.Should().Contain("tg://resolve?domain=ask_if_bot&start=");
            result.Should().NotContain("https://t.me/");
        }

        [Fact]
        public void Build_SupportsAtPrefixedChannelName()
        {
            var options = new TelegramOptions
            {
                LinkToChannel = "@ask_frankivsk",
                ChatInviteLink = "https://t.me/+InviteHash",
                BotUsername = "ask_if_bot"
            };

            PostTemplate.Build("Hello", options).Should().Contain("tg://resolve?domain=ask_frankivsk");
        }
    }
}
