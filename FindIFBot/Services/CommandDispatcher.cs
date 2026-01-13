using FindIFBot.Domain;
using FindIFBot.Handlers;
using FindIFBot.Persistence;
using FindIFBot.Services.Admin;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FindIFBot.Services
{
    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly ITelegramBotClient _bot;
        private readonly IUserSessionRepository _sessions;
        private readonly IMessageStore _messages;
        private readonly IAdminWorkflowService _admin;
        private readonly IStartHandler _startHandler;

        public CommandDispatcher(
            ITelegramBotClient bot,
            IUserSessionRepository sessions,
            IMessageStore messages,
            IAdminWorkflowService admin,
            IStartHandler startHandler)
        {
            _bot = bot;
            _sessions = sessions;
            _messages = messages;
            _admin = admin;
            _startHandler = startHandler;
        }

        public async Task DispatchAsync(Update update)
        {
            if (update.CallbackQuery != null)
            {
                await _admin.HandleCallbackAsync(update.CallbackQuery);

                return;
            }

            if (update.Message != null)
            {
                await HandleMessageAsync(update.Message);
            }
        }

        private async Task HandleMessageAsync(Message message)
        {
            var userId = message.From?.Id ?? message.Chat.Id;
            var session = _sessions.Get(userId);

            var text =
                message.Text?.Trim()
                ?? message.Caption?.Trim()
                ?? string.Empty;

            var normalized = text.ToLowerInvariant();

            _messages.Store(
                message.MessageId,
                new StoredMessage(
                    message.Chat.Id,
                    userId,
                    string.IsNullOrEmpty(text) ? null : text,
                    message.Photo != null
                )
            );

            if (normalized == "/start")
            {
                session.State = UserState.Idle;
                _sessions.Save(session);

                await _startHandler.HandleAsync(_bot, message);

                return;
            }

            switch (session.State)
            {
                case UserState.WaitingForFindQuery:
                    await _admin.SubmitFindAsync(message);
                    session.State = UserState.Idle;
                    _sessions.Save(session);

                    return;

                case UserState.WaitingForAdContent:
                    await _admin.SubmitAdAsync(message);
                    session.State = UserState.Idle;
                    _sessions.Save(session);

                    return;

                case UserState.WaitingForAdvice:
                    await HandleAdviceAsync(message, text);
                    session.State = UserState.Idle;
                    _sessions.Save(session);

                    return;
            }

            if (IsFindCommand(normalized))
            {
                session.State = UserState.WaitingForFindQuery;
                _sessions.Save(session);

                await _bot.SendMessage(
                    message.Chat.Id,
                    new FindHandler().Handle()
                );

                return;
            }

            if (IsAdsCommand(normalized))
            {
                session.State = UserState.WaitingForAdContent;
                _sessions.Save(session);

                await _bot.SendMessage(
                    message.Chat.Id,
                    new AdsHandler().Handle()
                );

                return;
            }

            if (IsAdviceCommand(normalized))
            {
                session.State = UserState.WaitingForAdvice;
                _sessions.Save(session);

                await _bot.SendMessage(
                    message.Chat.Id,
                    new IdeasHandler().Handle()
                );
                return;
            }

            await HandleStatelessCommandAsync(message, normalized);
        }

        private async Task HandleAdviceAsync(Message message, string text)
        {
            await _bot.SendMessage(
                message.Chat.Id,
                "Дякуємо за вашу ідею. Ми її опрацюємо."
            );

            await _bot.SendMessage(
                message.Chat.Id,
                new HelpHandler().Handle()
            );
        }

        private async Task HandleStatelessCommandAsync(Message message, string normalized)
        {
            ICommandHandler handler = normalized switch
            {
                "/help" or "довідка" => new HelpHandler(),
                "/ads_rule" or "/ads-rules" or "правила розміщення реклами" => new AdsRulesHandler(),
                "/donate" or "підтримати нас" => new SupportUsHandler(),
                _ => new UnknownHandler()
            };

            await _bot.SendMessage(
                message.Chat.Id,
                handler.Handle()
            );
        }

        private static bool IsFindCommand(string normalized) =>
            normalized == "/find" || normalized == "розпочати пошук";

        private static bool IsAdsCommand(string normalized) =>
            normalized == "/ads" || normalized == "розмістити рекламу";

        private static bool IsAdviceCommand(string normalized) =>
            normalized == "/advice" || normalized == "запропонувати покращення";
    }
}
