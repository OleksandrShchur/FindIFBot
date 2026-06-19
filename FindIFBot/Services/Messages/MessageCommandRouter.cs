using FindIFBot.EF.Repositories;
using FindIFBot.Handlers;
using FindIFBot.Helpers;
using FindIFBot.Helpers.Logs;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Services.Messages
{
    public class MessageCommandRouter : IMessageCommandRouter
    {
        private const string Component = "CommandRouter";
        private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };

        private readonly ITelegramBotClient _bot;
        private readonly IAsyncCommandHandler _historyHandler;
        private readonly IUserRequestHistoryRepository _history;
        private readonly IAppLogger<MessageCommandRouter> _logger;
        private readonly HelpHandler _helpHandler;
        private readonly PolicyHandler _policyHandler;
        private readonly SupportUsHandler _supportUsHandler;
        private readonly ChannelLinkHandler _channelLinkHandler;
        private readonly UnknownHandler _unknownHandler;

        public MessageCommandRouter(
            ITelegramBotClient bot,
            IEnumerable<IAsyncCommandHandler> handlers,
            IUserRequestHistoryRepository history,
            IAppLogger<MessageCommandRouter> logger,
            HelpHandler helpHandler,
            PolicyHandler policyHandler,
            SupportUsHandler supportUsHandler,
            ChannelLinkHandler channelLinkHandler,
            UnknownHandler unknownHandler)
        {
            _bot = bot;
            _historyHandler = handlers.OfType<HistoryHandler>().Single();
            _history = history;
            _logger = logger;
            _helpHandler = helpHandler;
            _policyHandler = policyHandler;
            _supportUsHandler = supportUsHandler;
            _channelLinkHandler = channelLinkHandler;
            _unknownHandler = unknownHandler;
        }

        public async Task RouteAsync(Message message, string normalized)
        {
            var userId = message.From!.Id;
            var hasHistory = await _history.HasHistory(userId);

            if (BotCommands.IsHistory(normalized))
            {
                await _historyHandler.HandleAsync(_bot, message);
                return;
            }

            ICommandHandler handler =
                BotCommands.IsHelp(normalized) ? _helpHandler :
                BotCommands.IsPolicy(normalized) ? _policyHandler :
                BotCommands.IsSupport(normalized) ? _supportUsHandler :
                BotCommands.IsChannel(normalized) ? _channelLinkHandler :
                _unknownHandler;

            await _logger.LogInfo(Component, $"Stateless command handled: {normalized} | UserId: {userId}");
            await _bot.SendMessage(
                message.Chat.Id,
                handler.Handle(),
                replyMarkup: Keyboards.GetKeyboard(hasHistory),
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html
            );
        }
    }
}
