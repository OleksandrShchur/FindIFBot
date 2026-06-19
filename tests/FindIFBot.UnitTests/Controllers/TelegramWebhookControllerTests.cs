using FindIFBot.Controllers;
using FindIFBot.Helpers.Logs;
using FindIFBot.Services;
using FindIFBot.UnitTests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace FindIFBot.UnitTests.Controllers
{
    public class TelegramWebhookControllerTests
    {
        private readonly ICommandDispatcher _dispatcher = Substitute.For<ICommandDispatcher>();
        private readonly IAppLogger<TelegramWebhookController> _logger = Substitute.For<IAppLogger<TelegramWebhookController>>();
        private readonly TelegramWebhookController _sut;

        public TelegramWebhookControllerTests()
        {
            _sut = new TelegramWebhookController(_dispatcher, _logger);
        }

        [Fact]
        public async Task Given_NullUpdate_When_Post_Then_Returns200AndDoesNotDispatch()
        {
            var result = await _sut.Post(null!);

            result.Should().BeOfType<OkResult>();
            await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!);
        }

        [Fact]
        public async Task Given_ValidUpdate_When_Post_Then_DispatchesAndReturns200()
        {
            var update = TelegramBuilder.MessageUpdate(TelegramBuilder.TextMessage("hi"));

            var result = await _sut.Post(update);

            result.Should().BeOfType<OkResult>();
            await _dispatcher.Received(1).DispatchAsync(update);
        }

        [Fact]
        public async Task Given_DispatcherThrows_When_Post_Then_SwallowsLogsAndReturns200()
        {
            var update = TelegramBuilder.MessageUpdate(TelegramBuilder.TextMessage("hi"));
            _dispatcher.DispatchAsync(update).Returns(Task.FromException(new InvalidOperationException("boom")));

            var result = await _sut.Post(update);

            result.Should().BeOfType<OkResult>();
            await _logger.Received(1).LogError("TelegramWebhookController", Arg.Any<string>());
        }
    }
}
