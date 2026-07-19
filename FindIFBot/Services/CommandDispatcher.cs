using FindIFBot.Helpers;
using FindIFBot.Services.Admin;
using FindIFBot.Services.Ask;
using FindIFBot.Services.Messages;
using Telegram.Bot.Types;

namespace FindIFBot.Services
{
    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly IAdminWorkflowService _adminWorkflow;
        private readonly IAskFlowService _askFlow;
        private readonly IMessageDispatchService _messageDispatch;

        public CommandDispatcher(
            IAdminWorkflowService adminWorkflow,
            IAskFlowService askFlow,
            IMessageDispatchService messageDispatch)
        {
            _adminWorkflow = adminWorkflow;
            _askFlow = askFlow;
            _messageDispatch = messageDispatch;
        }

        public async Task DispatchAsync(Update update)
        {
            if (update.CallbackQuery != null)
            {
                if (IsAskCallback(update.CallbackQuery))
                {
                    await _askFlow.HandleCallbackAsync(update.CallbackQuery);
                    return;
                }

                if (IsMainMenuCallback(update.CallbackQuery))
                {
                    await _askFlow.ReturnToMainMenuAsync(update.CallbackQuery);
                    return;
                }

                await _adminWorkflow.HandleCallbackAsync(update.CallbackQuery);
                return;
            }

            if (update.Message != null)
            {
                await _messageDispatch.HandleAsync(update.Message);
            }
        }

        private static bool IsAskCallback(CallbackQuery callback) =>
            BotCommands.IsAsk(BotCommands.Normalize(callback.Data));

        private static bool IsMainMenuCallback(CallbackQuery callback) =>
            BotCommands.IsMainMenu(BotCommands.Normalize(callback.Data));
    }
}
