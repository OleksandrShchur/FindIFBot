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

        private readonly ITelegramBotClient _bot;
        private readonly IAsyncCommandHandler _historyHandler;
        private readonly IUserRequestHistoryRepository _history;
        private readonly IAppLogger<MessageCommandRouter> _logger;
        private readonly SupportUsHandler _supportUsHandler;
        private readonly ChannelLinkHandler _channelLinkHandler;

        public MessageCommandRouter(
            ITelegramBotClient bot,
            IEnumerable<IAsyncCommandHandler> handlers,
            IUserRequestHistoryRepository history,
            IAppLogger<MessageCommandRouter> logger,
            SupportUsHandler supportUsHandler,
            ChannelLinkHandler channelLinkHandler)
        {
            _bot = bot;
            _historyHandler = handlers.OfType<HistoryHandler>().Single();
            _history = history;
            _logger = logger;
            _supportUsHandler = supportUsHandler;
            _channelLinkHandler = channelLinkHandler;
        }

        public async Task RouteAsync(Message message, string normalized)
        {
            var userId = message.From!.Id;
            var hasHistory = await _history.HasHistory(userId);

            if (normalized == "📋 історія запитів" || normalized == "історія запитів" || normalized == "/history")
            {
                await _historyHandler.HandleAsync(_bot, message);
                return;
            }

            ICommandHandler handler = normalized switch
            {
                "/help" or "ℹ️ довідка" or "довідка" => new HelpHandler(),
                "/policy" or "📜 правила" or "правила" => new PolicyHandler(),
                "/support" or "❤️ підтримати" or "підтримати" => _supportUsHandler,
                "/channel" or "🔗 канал" or "канал" => _channelLinkHandler,
                _ => new UnknownHandler()
            };

            await _logger.LogInfo(Component, $"Stateless command handled: {normalized} | UserId: {userId}");
            await _bot.SendMessage(
                message.Chat.Id,
                handler.Handle(),
                replyMarkup: Keyboards.GetKeyboard(hasHistory),
                parseMode: ParseMode.Html
            );
        }
    }
}
