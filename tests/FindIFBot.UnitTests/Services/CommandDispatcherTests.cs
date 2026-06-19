using FindIFBot.Services;
using FindIFBot.Services.Admin;
using FindIFBot.Services.Ask;
using FindIFBot.Services.Messages;
using FindIFBot.UnitTests.TestSupport;

namespace FindIFBot.UnitTests.Services
{
    public class CommandDispatcherTests
    {
        private readonly IAdminWorkflowService _adminWorkflow = Substitute.For<IAdminWorkflowService>();
        private readonly IAskFlowService _askFlow = Substitute.For<IAskFlowService>();
        private readonly IMessageDispatchService _messageDispatch = Substitute.For<IMessageDispatchService>();
        private readonly CommandDispatcher _sut;

        public CommandDispatcherTests()
        {
            _sut = new CommandDispatcher(_adminWorkflow, _askFlow, _messageDispatch);
        }

        [Fact]
        public async Task Given_AskCallbackQuery_When_Dispatch_Then_RoutesToAskFlow()
        {
            var callback = TelegramBuilder.CallbackQuery("/ask");
            var update = TelegramBuilder.CallbackUpdate(callback);

            await _sut.DispatchAsync(update);

            await _askFlow.Received(1).HandleCallbackAsync(callback);
            await _adminWorkflow.DidNotReceive().HandleCallbackAsync(Arg.Any<Telegram.Bot.Types.CallbackQuery>());
            await _messageDispatch.DidNotReceive().HandleAsync(Arg.Any<Telegram.Bot.Types.Message>());
        }

        [Fact]
        public async Task Given_NonAskCallbackQuery_When_Dispatch_Then_RoutesToAdminWorkflow()
        {
            var callback = TelegramBuilder.CallbackQuery("approve:42");
            var update = TelegramBuilder.CallbackUpdate(callback);

            await _sut.DispatchAsync(update);

            await _adminWorkflow.Received(1).HandleCallbackAsync(callback);
            await _askFlow.DidNotReceive().HandleCallbackAsync(Arg.Any<Telegram.Bot.Types.CallbackQuery>());
        }

        [Fact]
        public async Task Given_MessageUpdate_When_Dispatch_Then_RoutesToMessageDispatch()
        {
            var message = TelegramBuilder.TextMessage("hello");
            var update = TelegramBuilder.MessageUpdate(message);

            await _sut.DispatchAsync(update);

            await _messageDispatch.Received(1).HandleAsync(message);
            await _askFlow.DidNotReceive().HandleCallbackAsync(Arg.Any<Telegram.Bot.Types.CallbackQuery>());
            await _adminWorkflow.DidNotReceive().HandleCallbackAsync(Arg.Any<Telegram.Bot.Types.CallbackQuery>());
        }

        [Fact]
        public async Task Given_UnsupportedUpdate_When_Dispatch_Then_DoesNothingAndDoesNotThrow()
        {
            var update = TelegramBuilder.EmptyUpdate();

            var act = async () => await _sut.DispatchAsync(update);

            await act.Should().NotThrowAsync();
            await _messageDispatch.DidNotReceive().HandleAsync(Arg.Any<Telegram.Bot.Types.Message>());
            await _askFlow.DidNotReceive().HandleCallbackAsync(Arg.Any<Telegram.Bot.Types.CallbackQuery>());
            await _adminWorkflow.DidNotReceive().HandleCallbackAsync(Arg.Any<Telegram.Bot.Types.CallbackQuery>());
        }
    }
}
