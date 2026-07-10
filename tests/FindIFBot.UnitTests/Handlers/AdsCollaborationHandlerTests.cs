using FindIFBot.Configuration;
using FindIFBot.Handlers;
using FindIFBot.UnitTests.TestSupport;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.UnitTests.Handlers
{
    public class AdsCollaborationHandlerTests
    {
        private const long ChatId = 4242;
        private const string DirectLink = "https://t.me/ask_frankivsk?direct";

        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly AdsCollaborationHandler _sut;

        public AdsCollaborationHandlerTests()
        {
            var options = Options.Create(new TelegramOptions { DirectChatLink = DirectLink });
            _sut = new AdsCollaborationHandler(options);
        }

        [Fact]
        public async Task Given_Message_When_HandleAsync_Then_SendsPolicyWithDirectRedirectButton()
        {
            var message = TelegramBuilder.TextMessage("🤝 Реклама та співпраця", chatId: ChatId);

            await _sut.HandleAsync(_bot, message);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ChatId.Identifier.Should().Be(ChatId);
            sent.Text.Should().Contain("Реклама та співпраця");
            sent.Text.Should().Contain("Вартість");

            var keyboard = sent.ReplyMarkup.Should().BeOfType<InlineKeyboardMarkup>().Subject;
            var button = keyboard.InlineKeyboard.SelectMany(row => row).Single();
            button.Url.Should().Be(DirectLink);
        }
    }
}
