using FindIFBot.Configuration;
using FindIFBot.Persistence;
using FindIFBot.Services.Admin;
using FindIFBot.UnitTests.TestSupport;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace FindIFBot.UnitTests.Services.Admin
{
    public class RequestPublisherTests
    {
        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly RequestPublisher _sut;

        public RequestPublisherTests()
        {
            _bot.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(new Message { Id = 42 });
            _bot.SendRequest(Arg.Any<SendPhotoRequest>(), Arg.Any<CancellationToken>())
                .Returns(new Message { Id = 42 });

            _sut = new RequestPublisher(_bot, Options.Create(new TelegramOptions
            {
                UserOutputChannel = "@ask_frankivsk",
                LinkToChannel = "https://t.me/ask_frankivsk",
                ChatInviteLink = "https://t.me/+YAgDZDhECi00M2Yy",
                BotUsername = "ask_if_bot"
            }));
        }

        [Fact]
        public async Task PublishAsync_TextOnly_DisablesLinkPreview()
        {
            var stored = new StoredMessage(1, 2, "Test post", [], null, 100);

            await _sut.PublishAsync(stored);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.LinkPreviewOptions.Should().NotBeNull();
            sent.LinkPreviewOptions!.IsDisabled.Should().BeTrue();
            sent.Text.Should().NotContain("https://t.me/");
        }

        [Fact]
        public async Task PublishAsync_TextWithTextLink_PreservesAnchorInBody()
        {
            var entities = new[]
            {
                new StoredMessageEntity("TextLink", 5, 4, Url: "https://example.com/x")
            };
            var stored = new StoredMessage(1, 2, "Test post", [], null, 100, entities);

            await _sut.PublishAsync(stored);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ParseMode.Should().Be(Telegram.Bot.Types.Enums.ParseMode.Html);
            sent.Text.Should().StartWith("Test <a href=\"https://example.com/x\">post</a>");
            sent.Text.Should().Contain("tg://resolve?domain=ask_frankivsk");
        }

        [Fact]
        public async Task PublishAsync_SinglePhoto_UsesDeepLinksInCaption()
        {
            var stored = new StoredMessage(1, 2, "Photo post", ["photo-file-id"], null, 100);

            await _sut.PublishAsync(stored);

            var sent = _bot.SingleRequest<SendPhotoRequest>();
            sent.Caption.Should().Contain("tg://resolve?domain=ask_frankivsk");
            sent.Caption.Should().NotContain("https://t.me/");
        }
    }
}
