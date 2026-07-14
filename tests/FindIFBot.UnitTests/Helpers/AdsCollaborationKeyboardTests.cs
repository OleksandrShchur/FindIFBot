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

        [Fact]
        public void Given_AdminKeyboard_When_Built_Then_IncludesPendingQueueButton()
        {
            var keyboard = (ReplyKeyboardMarkup)Keyboards.GetKeyboard(hasHistory: false, isAdmin: true);
            var texts = keyboard.Keyboard.SelectMany(r => r).Select(b => b.Text).ToList();

            texts.Should().Contain(Keyboards.AdminPendingCaption);
        }

        [Fact]
        public void Given_AdminKeyboardWithHistory_When_Built_Then_HistoryAndPendingShareOneRow()
        {
            var keyboard = (ReplyKeyboardMarkup)Keyboards.GetKeyboard(hasHistory: true, isAdmin: true);
            var rows = keyboard.Keyboard.Select(r => r.ToArray()).ToArray();

            var sharedRow = rows.Single(r => r.Any(b => b.Text.Contains("Історія")));
            sharedRow.Select(b => b.Text).Should().Equal("📋 Історія запитів", Keyboards.AdminPendingCaption);
        }

        [Fact]
        public void Given_NonAdminKeyboard_When_Built_Then_OmitsPendingQueueButton()
        {
            var keyboard = (ReplyKeyboardMarkup)Keyboards.GetKeyboard(hasHistory: true, isAdmin: false);
            var texts = keyboard.Keyboard.SelectMany(r => r).Select(b => b.Text).ToList();

            texts.Should().NotContain(Keyboards.AdminPendingCaption);
        }

        [Theory]
        [InlineData("/pending")]
        [InlineData("⏳ черга модерації")]
        [InlineData("черга модерації")]
        public void Given_PendingTrigger_When_IsPending_Then_ReturnsTrue(string normalized)
        {
            BotCommands.IsPending(normalized).Should().BeTrue();
        }
    }
}
