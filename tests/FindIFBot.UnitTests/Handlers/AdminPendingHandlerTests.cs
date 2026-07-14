using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Handlers;
using FindIFBot.Helpers;
using FindIFBot.UnitTests.TestSupport;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.UnitTests.Handlers
{
    public class AdminPendingHandlerTests
    {
        private const long AdminId = 1000;
        private const long OtherUserId = 200;

        private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
        private readonly IUserRequestHistoryRepository _history = Substitute.For<IUserRequestHistoryRepository>();
        private readonly AdminPendingHandler _sut;

        public AdminPendingHandlerTests()
        {
            _sut = new AdminPendingHandler(
                _history,
                Options.Create(new HistoryOptions { MaxItemsPerSection = 10 }),
                Options.Create(new TelegramOptions { AdminId = AdminId }));
        }

        [Fact]
        public async Task Given_NonAdmin_When_Handle_Then_DeniesAccess()
        {
            _history.HasHistory(OtherUserId).Returns(false);
            var message = TelegramBuilder.TextMessage("/pending", userId: OtherUserId, chatId: OtherUserId);

            await _sut.HandleAsync(_bot, message);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.Text.Should().Contain("лише адміністратору");
            await _history.DidNotReceive().GetPendingAsync(Arg.Any<int>());
        }

        [Fact]
        public async Task Given_AdminWithEmptyQueue_When_Handle_Then_SendsEmptyMessage()
        {
            _history.HasHistory(AdminId).Returns(true);
            _history.GetPendingAsync(10).Returns([]);
            var message = TelegramBuilder.TextMessage(Keyboards.AdminPendingCaption, userId: AdminId, chatId: AdminId);

            await _sut.HandleAsync(_bot, message);

            var sent = _bot.SingleRequest<SendMessageRequest>();
            sent.Text.Should().Contain("Черга модерації порожня");
            var keyboard = sent.ReplyMarkup.Should().BeOfType<ReplyKeyboardMarkup>().Subject;
            keyboard.Keyboard.SelectMany(r => r).Should()
                .Contain(b => b.Text == Keyboards.AdminPendingCaption);
        }

        [Fact]
        public async Task Given_AdminWithPending_When_Handle_Then_RepliesToAdminInfoMessage()
        {
            _history.HasHistory(AdminId).Returns(true);
            _history.GetPendingAsync(10).Returns(
            [
                new UserRequest
                {
                    Id = Guid.NewGuid(),
                    UserId = 50,
                    UserMessageId = 234,
                    Status = RequestStatus.Pending,
                    SubmittedAt = DateTime.UtcNow,
                    AdminInfoMessageId = 777
                }
            ]);
            var message = TelegramBuilder.TextMessage("/pending", userId: AdminId, chatId: AdminId);

            await _sut.HandleAsync(_bot, message);

            var sent = _bot.SentRequests<SendMessageRequest>();
            sent.Should().HaveCount(2);
            sent[0].Text.Should().Contain("Запити в черзі");
            sent[1].Text.Should().Contain("234");
            sent[1].Text.Should().Contain("очікує дії адміна");
            sent[1].ReplyParameters!.MessageId.Should().Be(777);
        }

        [Fact]
        public async Task Given_PendingWithoutAdminInfoMessageId_When_Handle_Then_SendsWithoutReply()
        {
            _history.HasHistory(AdminId).Returns(true);
            _history.GetPendingAsync(10).Returns(
            [
                new UserRequest
                {
                    Id = Guid.NewGuid(),
                    UserId = 50,
                    UserMessageId = 11,
                    Status = RequestStatus.Pending,
                    SubmittedAt = DateTime.UtcNow,
                    AdminInfoMessageId = null
                }
            ]);
            var message = TelegramBuilder.TextMessage("/pending", userId: AdminId, chatId: AdminId);

            await _sut.HandleAsync(_bot, message);

            var item = _bot.SentRequests<SendMessageRequest>().Last();
            item.Text.Should().Contain("11");
            item.Text.Should().Contain("відсутнє");
            item.ReplyParameters.Should().BeNull();
        }
    }
}
