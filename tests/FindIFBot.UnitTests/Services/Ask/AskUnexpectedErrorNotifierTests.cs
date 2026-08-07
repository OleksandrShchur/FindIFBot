using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers.Logs;
using FindIFBot.Services.Ask;
using FindIFBot.UnitTests.TestSupport;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.UnitTests.Services.Ask
{
    public class AskUnexpectedErrorNotifierTests
    {
        private const long UserId = 555;
        private const long ChatId = 777;
        private const string DirectLink = "https://t.me/ask_frankivsk?direct";

        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly IUserSessionRepository _sessions = Substitute.For<IUserSessionRepository>();
        private readonly IAppLogger<AskUnexpectedErrorNotifier> _logger =
            Substitute.For<IAppLogger<AskUnexpectedErrorNotifier>>();
        private readonly AskUnexpectedErrorNotifier _sut;

        public AskUnexpectedErrorNotifierTests()
        {
            _sessions.GetAsync(UserId).Returns(new UserSession
            {
                UserId = UserId,
                State = UserState.WaitingForAskQuery
            });

            _sut = new AskUnexpectedErrorNotifier(
                _bot,
                _sessions,
                Options.Create(new TelegramOptions { DirectChatLink = DirectLink }),
                _logger);
        }

        [Fact]
        public async Task NotifyAsync_ResetsSessionToIdle_AndSendsUkrainianMessageWithDirectChatButton()
        {
            await _sut.NotifyAsync(ChatId, UserId);

            await _sessions.Received(1).SaveAsync(Arg.Is<UserSession>(s =>
                s.UserId == UserId && s.State == UserState.Idle));

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.ChatId.Identifier.Should().Be(ChatId);
            sent.ParseMode.Should().Be(ParseMode.Html);
            sent.Text.Should().Be(AskUnexpectedErrorNotifier.MessageText);
            sent.Text.Should().Contain("неочікувана помилка");
            sent.Text.Should().Contain("дірект");

            var keyboard = sent.ReplyMarkup.Should().BeOfType<InlineKeyboardMarkup>().Subject;
            var button = keyboard.InlineKeyboard.SelectMany(r => r).Should().ContainSingle().Subject;
            button.Text.Should().Be("✍️ Написати в дірект");
            button.Url.Should().Be(DirectLink);
        }

        [Fact]
        public async Task NotifyAsync_WhenSessionSaveFails_StillSendsMessageAndDoesNotThrow()
        {
            _sessions.SaveAsync(Arg.Any<UserSession>())
                .Returns<Task>(_ => throw new InvalidOperationException("db down"));

            await _sut.Invoking(s => s.NotifyAsync(ChatId, UserId)).Should().NotThrowAsync();

            _bot.SentRequests<SendMessageRequest>().Should().ContainSingle();
            await _logger.Received().LogError("AskUnexpectedError", Arg.Is<string>(m => m.Contains("reset session")));
        }

        [Fact]
        public async Task NotifyAsync_WhenSendMessageFails_LogsAndDoesNotThrow()
        {
            _bot.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns<Message>(_ => throw new InvalidOperationException("telegram down"));

            await _sut.Invoking(s => s.NotifyAsync(ChatId, UserId)).Should().NotThrowAsync();

            await _sessions.Received(1).SaveAsync(Arg.Is<UserSession>(s => s.State == UserState.Idle));
            await _logger.Received().LogError("AskUnexpectedError", Arg.Is<string>(m => m.Contains("send unexpected")));
        }
    }
}
