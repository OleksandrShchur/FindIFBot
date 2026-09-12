using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers.Logs;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Services.Ask
{
    public class AskUnexpectedErrorNotifier : IAskUnexpectedErrorNotifier
    {
        private const string Component = "AskUnexpectedError";
        private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };

        public const string MessageText =
            "⚠️ <b>Сталася неочікувана помилка</b>\n\n" +
            "Наша команда вже працює над виправленням.\n\n" +
            "Щоб продовжити розмову, будь ласка, напишіть нам у дірект.";

        private readonly ITelegramBotClient _bot;
        private readonly IUserSessionRepository _sessions;
        private readonly TelegramOptions _options;
        private readonly IAppLogger<AskUnexpectedErrorNotifier> _logger;

        public AskUnexpectedErrorNotifier(
            ITelegramBotClient bot,
            IUserSessionRepository sessions,
            IOptions<TelegramOptions> options,
            IAppLogger<AskUnexpectedErrorNotifier> logger)
        {
            _bot = bot;
            _sessions = sessions;
            _options = options.Value;
            _logger = logger;
        }

        public async Task NotifyAsync(long chatId, long userId)
        {
            try
            {
                var session = await _sessions.GetAsync(userId);
                session.State = UserState.Idle;
                await _sessions.SaveAsync(session);
            }
            catch (Exception ex)
            {
                await _logger.LogError(Component,
                    $"Failed to reset session after unexpected ask error | UserId: {userId} | Error: {ex.Message}");
            }

            try
            {
                var keyboard = new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithUrl("✍️ Написати в дірект", _options.DirectChatLink));

                await _bot.SendMessage(
                    chatId,
                    MessageText,
                    replyMarkup: keyboard,
                    linkPreviewOptions: NoPreview,
                    parseMode: ParseMode.Html
                );
            }
            catch (Exception ex)
            {
                await _logger.LogError(Component,
                    $"Failed to send unexpected ask error message | UserId: {userId} | ChatId: {chatId} | Error: {ex.Message}");
            }
        }
    }
}
