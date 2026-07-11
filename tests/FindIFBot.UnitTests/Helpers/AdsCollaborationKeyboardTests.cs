using FindIFBot.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.UnitTests.Helpers
{
    public class AdsCollaborationKeyboardTests
    {
        private const string AdsCaption = "🤝 Реклама та співпраця";

        [Theory]
        [InlineData("🤝 реклама та співпраця")]
        [InlineData("реклама та співпраця")]
        public void Given_AdsCaptionOrText_When_IsAdsCollaboration_Then_ReturnsTrue(string normalized)
        {
            BotCommands.IsAdsCollaboration(normalized).Should().BeTrue();
        }

        [Fact]
        public void Given_UnrelatedText_When_IsAdsCollaboration_Then_ReturnsFalse()
        {
            BotCommands.IsAdsCollaboration("надіслати запит").Should().BeFalse();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Given_Keyboard_When_Built_Then_AdsButtonIsFullWidthRowUnderSendRequest(bool hasHistory)
        {
            var keyboard = (ReplyKeyboardMarkup)Keyboards.GetKeyboard(hasHistory);
            var rows = keyboard.Keyboard.Select(r => r.ToArray()).ToArray();

            rows[0].Should().ContainSingle().Which.Text.Should().Be("📨 Надіслати запит");

            var adsRow = rows[1];
            adsRow.Should().ContainSingle().Which.Text.Should().Be(AdsCaption);

            if (hasHistory)
            {
                var adsIndex = Array.FindIndex(rows, r => r.Any(b => b.Text == AdsCaption));
                var historyIndex = Array.FindIndex(rows, r => r.Any(b => b.Text.Contains("Історія")));
                adsIndex.Should().BeLessThan(historyIndex);
            }
        }
    }
}
