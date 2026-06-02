using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Handlers;
using FindIFBot.Helpers.Logs;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Services.Ask
{
    public class AskFlowService : IAskFlowService
    {
        private const string Component = "AskFlow";

        private readonly ITelegramBotClient _bot;
        private readonly IUserSessionRepository _sessions;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IAppLogger<AskFlowService> _logger;
        private readonly TelegramOptions _options;

        public AskFlowService(
            ITelegramBotClient bot,
            IUserSessionRepository sessions,
            ISubscriptionService subscriptionService,
            IAppLogger<AskFlowService> logger,
            IOptions<TelegramOptions> options)
        {
            _bot = bot;
            _sessions = sessions;
            _subscriptionService = subscriptionService;
            _logger = logger;
            _options = options.Value;
        }

        public async Task HandleCallbackAsync(CallbackQuery callback)
        {
            await _bot.AnswerCallbackQuery(callback.Id);

            var userId = callback.From.Id;
            var chatId = callback.Message?.Chat.Id ?? userId;
            var session = _sessions.Get(userId);

            await StartAsync(chatId, userId, session);
        }

        public async Task StartAsync(long chatId, long userId, UserSession session)
        {
            if (!await _subscriptionService.IsSubscribedToOutputChannelAsync(userId))
            {
                session.State = UserState.Idle;
                _sessions.Save(session);

                await SendSubscriptionRequiredMessageAsync(chatId);
                await _logger.LogInfo(Component, $"Ask flow blocked: user is not subscribed to output channel | UserId: {userId}");

                return;
            }

            session.State = UserState.WaitingForAskQuery;
            _sessions.Save(session);
            await _logger.LogInfo(Component, $"User started ask flow | UserId: {userId}");

            await _bot.SendMessage(
                chatId,
                new AskHandler().Handle(),
                replyMarkup: new ReplyKeyboardRemove(),
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
                parseMode: ParseMode.Html
            );
        }

        private async Task SendSubscriptionRequiredMessageAsync(long chatId)
        {
            var keyboard = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithUrl("🔗 Підписатися на канал", _options.LinkToChannel));

            await _bot.SendMessage(
                chatId,
                "🔒 <b>Щоб надіслати запит, потрібно бути підписаним на наш канал.</b>\n\n" +
                "Будь ласка, підпишіться на канал і після цього знову натисніть «📨 Надіслати запит» або введіть /ask.",
                replyMarkup: keyboard,
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
                parseMode: ParseMode.Html
            );
        }
    }
}
