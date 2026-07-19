using FindIFBot.Persistence;
using FindIFBot.Utils;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.UnitTests.Utils
{
    public class MessageEntityHtmlTests
    {
        [Fact]
        public void Format_EmptyText_ReturnsEmpty()
        {
            MessageEntityHtml.Format(null, null).Should().BeEmpty();
            MessageEntityHtml.Format("", []).Should().BeEmpty();
        }

        [Fact]
        public void Format_NoEntities_EscapesHtml()
        {
            MessageEntityHtml.Format("a <b> & c", null)
                .Should().Be("a &lt;b&gt; &amp; c");
        }

        [Fact]
        public void Format_TextLink_WrapsAnchor()
        {
            var text = "Click here please";
            var entities = new[]
            {
                new StoredMessageEntity("TextLink", 6, 4, Url: "https://example.com/path")
            };

            MessageEntityHtml.Format(text, entities)
                .Should().Be("Click <a href=\"https://example.com/path\">here</a> please");
        }

        [Fact]
        public void Format_UrlEntity_LinksSubstring()
        {
            var text = "See https://t.me/x now";
            var entities = new[]
            {
                new StoredMessageEntity("Url", 4, 14)
            };

            MessageEntityHtml.Format(text, entities)
                .Should().Be("See <a href=\"https://t.me/x\">https://t.me/x</a> now");
        }

        [Fact]
        public void Format_BoldInsideTextLink_NestsTags()
        {
            var text = "Go HOME";
            var entities = new[]
            {
                new StoredMessageEntity("TextLink", 3, 4, Url: "https://example.com"),
                new StoredMessageEntity("Bold", 3, 4)
            };

            MessageEntityHtml.Format(text, entities)
                .Should().Be("Go <a href=\"https://example.com\"><b>HOME</b></a>");
        }

        [Fact]
        public void Format_MixedFormatting_PreservesOrder()
        {
            var text = "bold and italic";
            var entities = new[]
            {
                new StoredMessageEntity("Bold", 0, 4),
                new StoredMessageEntity("Italic", 9, 6)
            };

            MessageEntityHtml.Format(text, entities)
                .Should().Be("<b>bold</b> and <i>italic</i>");
        }

        [Fact]
        public void FromTelegram_MapsTextLinkFields()
        {
            var telegram = new[]
            {
                new MessageEntity
                {
                    Type = MessageEntityType.TextLink,
                    Offset = 1,
                    Length = 2,
                    Url = "https://a.b"
                }
            };

            var stored = MessageEntityHtml.FromTelegram(telegram);

            stored.Should().ContainSingle();
            stored[0].Type.Should().Be("TextLink");
            stored[0].Offset.Should().Be(1);
            stored[0].Length.Should().Be(2);
            stored[0].Url.Should().Be("https://a.b");
        }
    }
}
