using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Handlers;
using FindIFBot.UnitTests.TestSupport;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.UnitTests.Handlers
{
    public class HistoryHandlerTests
    {
        private const long UserId = 100;
        private const long AdminId = 999;
        private const long ChatId = 100;

        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly IUserRequestHistoryRepository _history = Substitute.For<IUserRequestHistoryRepository>();
        private readonly HistoryHandler _sut;

        public HistoryHandlerTests()
        {
            _bot.SendRequest(Arg.Any<SendRichMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(_ => new Message { Id = 1 });
            _bot.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(_ => new Message { Id = 2 });

            _sut = new HistoryHandler(
                _history,
                Options.Create(new HistoryOptions { MaxItemsPerSection = 10 }),
                Options.Create(new TelegramOptions { AdminId = AdminId }));
        }

        [Fact]
        public async Task Given_EmptyHistory_When_Handle_Then_SendsEmptyMessageWithoutStats()
        {
            _history.GetByUserId(UserId).Returns([]);
            var message = TelegramBuilder.TextMessage("📋 Історія запитів", userId: UserId, chatId: ChatId);

            await _sut.HandleAsync(_bot, message);

            _bot.SentRequests<SendRichMessageRequest>().Should().BeEmpty();
            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.Text.Should().Contain("У вас ще немає історії запитів");
            await _history.DidNotReceive().GetStatusCountsByUserIdAsync(Arg.Any<long>());
        }

        [Fact]
        public async Task Given_Requests_When_Handle_Then_SendsStatsRichMessageFirst()
        {
            _history.GetByUserId(UserId).Returns(
            [
                Request(RequestStatus.Approved, userMessageId: 10, channelLink: "https://t.me/c/1/1")
            ]);
            _history.GetStatusCountsByUserIdAsync(UserId)
                .Returns(new Dictionary<RequestStatus, int>
                {
                    [RequestStatus.Approved] = 1
                });
            var message = TelegramBuilder.TextMessage("/history", userId: UserId, chatId: ChatId);

            await _sut.HandleAsync(_bot, message);

            var rich = _bot.SentRequests<SendRichMessageRequest>().Should().ContainSingle().Subject;
            rich.ChatId.Identifier.Should().Be(ChatId);
            rich.ReplyMarkup.Should().BeNull();

            var html = rich.RichMessage.Html!;
            html.Should().Contain("<b>Статистика запитів</b>");
            html.Should().Contain("<th>Статус</th><th>Кількість</th>");
            html.Should().Contain("<tr><td>Затверджені</td><td>1</td></tr>");
            html.Should().Contain("<tr><td><b>Всього</b></td><td><b>1</b></td></tr>");

            var history = _bot.SentRequests<SendMessageRequest>().Should().ContainSingle().Subject;
            history.Text.Should().Contain("Затверджені запити");
        }

        [Fact]
        public async Task Given_MixedStatusCounts_When_Handle_Then_OmitsZeroStatusesAndUsesUkrainianLabels()
        {
            _history.GetByUserId(UserId).Returns(
            [
                Request(RequestStatus.Pending, userMessageId: 42),
                Request(RequestStatus.Approved, userMessageId: 41, channelLink: "https://t.me/c/1/2"),
                Request(RequestStatus.Rejected, userMessageId: 40)
            ]);
            _history.GetStatusCountsByUserIdAsync(UserId)
                .Returns(new Dictionary<RequestStatus, int>
                {
                    [RequestStatus.Pending] = 1,
                    [RequestStatus.Approved] = 2,
                    [RequestStatus.Rejected] = 1
                });
            var message = TelegramBuilder.TextMessage("/history", userId: UserId, chatId: ChatId);

            await _sut.HandleAsync(_bot, message);

            var html = _bot.SentRequests<SendRichMessageRequest>().Single().RichMessage.Html!;
            html.Should().Contain("<tr><td>На модерації</td><td>1</td></tr>");
            html.Should().Contain("<tr><td>Затверджені</td><td>2</td></tr>");
            html.Should().Contain("<tr><td>Відхилені</td><td>1</td></tr>");
            html.Should().Contain("<tr><td><b>Всього</b></td><td><b>4</b></td></tr>");
            html.Should().NotContain("Дублікати");
            html.Should().NotContain("Реклама");
            html.Should().NotContain("Уточнення");
            html.Should().NotContain("Pending");
            html.Should().NotContain("Approved");
            html.Should().NotContain("Total");
        }

        [Fact]
        public async Task Given_AllStatuses_When_Handle_Then_RendersUkrainianLabelsInOrder()
        {
            _history.GetByUserId(UserId).Returns(
            [
                Request(RequestStatus.Approved, userMessageId: 1, channelLink: "https://t.me/c/1/3")
            ]);
            _history.GetStatusCountsByUserIdAsync(UserId)
                .Returns(new Dictionary<RequestStatus, int>
                {
                    [RequestStatus.NeedsAttention] = 1,
                    [RequestStatus.Advertisement] = 1,
                    [RequestStatus.Duplicate] = 1,
                    [RequestStatus.Rejected] = 1,
                    [RequestStatus.Approved] = 1,
                    [RequestStatus.Pending] = 1
                });
            var message = TelegramBuilder.TextMessage("/history", userId: UserId, chatId: ChatId);

            await _sut.HandleAsync(_bot, message);

            var html = _bot.SentRequests<SendRichMessageRequest>().Single().RichMessage.Html!;
            var pending = html.IndexOf("<tr><td>На модерації</td><td>1</td></tr>", StringComparison.Ordinal);
            var approved = html.IndexOf("<tr><td>Затверджені</td><td>1</td></tr>", StringComparison.Ordinal);
            var rejected = html.IndexOf("<tr><td>Відхилені</td><td>1</td></tr>", StringComparison.Ordinal);
            var duplicate = html.IndexOf("<tr><td>Дублікати</td><td>1</td></tr>", StringComparison.Ordinal);
            var ads = html.IndexOf("<tr><td>Реклама</td><td>1</td></tr>", StringComparison.Ordinal);
            var attention = html.IndexOf("<tr><td>Уточнення</td><td>1</td></tr>", StringComparison.Ordinal);
            var total = html.IndexOf("<tr><td><b>Всього</b></td><td><b>6</b></td></tr>", StringComparison.Ordinal);

            pending.Should().BeGreaterThan(-1);
            approved.Should().BeGreaterThan(pending);
            rejected.Should().BeGreaterThan(approved);
            duplicate.Should().BeGreaterThan(rejected);
            ads.Should().BeGreaterThan(duplicate);
            attention.Should().BeGreaterThan(ads);
            total.Should().BeGreaterThan(attention);
        }

        [Fact]
        public async Task Given_ApprovedAndPending_When_Handle_Then_StatsThenHistoryMessages()
        {
            _history.GetByUserId(UserId).Returns(
            [
                Request(RequestStatus.Approved, userMessageId: 10, channelLink: "https://t.me/c/1/4"),
                Request(RequestStatus.Pending, userMessageId: 11)
            ]);
            _history.GetStatusCountsByUserIdAsync(UserId)
                .Returns(new Dictionary<RequestStatus, int>
                {
                    [RequestStatus.Approved] = 1,
                    [RequestStatus.Pending] = 1
                });
            var message = TelegramBuilder.TextMessage("/history", userId: UserId, chatId: ChatId);

            await _sut.HandleAsync(_bot, message);

            var calls = _bot.ReceivedCalls()
                .Where(c => c.GetMethodInfo().Name == nameof(ITelegramBotClient.SendRequest))
                .Select(c => c.GetArguments()[0])
                .ToList();

            calls[0].Should().BeOfType<SendRichMessageRequest>();
            calls.OfType<SendMessageRequest>().Should().HaveCountGreaterThanOrEqualTo(2);

            var historyMessages = _bot.SentRequests<SendMessageRequest>();
            historyMessages[0].Text.Should().Contain("Затверджені запити");
            historyMessages[1].Text.Should().Contain("Запити на модерації");
            historyMessages[1].ReplyMarkup.Should().BeOfType<ReplyKeyboardMarkup>();
        }

        [Fact]
        public async Task Given_OnlyRejected_When_Handle_Then_SendsStatsWithoutHistorySections()
        {
            _history.GetByUserId(UserId).Returns(
            [
                Request(RequestStatus.Rejected, userMessageId: 55)
            ]);
            _history.GetStatusCountsByUserIdAsync(UserId)
                .Returns(new Dictionary<RequestStatus, int>
                {
                    [RequestStatus.Rejected] = 1
                });
            var message = TelegramBuilder.TextMessage("/history", userId: UserId, chatId: ChatId);

            await _sut.HandleAsync(_bot, message);

            _bot.SentRequests<SendRichMessageRequest>().Should().ContainSingle();
            _bot.SentRequests<SendMessageRequest>().Should().BeEmpty();
        }

        private static UserRequest Request(
            RequestStatus status,
            int userMessageId,
            string? channelLink = null) =>
            new()
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                Status = status,
                UserMessageId = userMessageId,
                ChannelLink = channelLink,
                SubmittedAt = DateTime.UtcNow
            };
    }
}
