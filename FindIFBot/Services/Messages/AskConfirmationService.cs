using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers.Logs;
using FindIFBot.Persistence;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Services.Messages
{
    public class AskConfirmationService : IAskConfirmationService
    {
        private const string Component = "AskConfirmation";

        private readonly ITelegramBotClient _bot;
        private readonly IMessageStore _messages;
        private readonly IUserSessionRepository _sessions;
        private readonly IAppLogger<AskConfirmationService> _logger;

        public AskConfirmationService(
            ITelegramBotClient bot,
            IMessageStore messages,
            IUserSessionRepository sessions,
            IAppLogger<AskConfirmationService> logger)
        {
            _bot = bot;
            _messages = messages;
            _sessions = sessions;
            _logger = logger;
        }

        public async Task SendConfirmationAsync(Message message, UserSession session)
        {
            await _logger.LogInfo(Component,
                $"Preparing ask confirmation | UserId: {message.From!.Id} | MessageId: {message.MessageId}");

            if (!_messages.TryGet(message.MessageId, out var stored))
            {
                await _logger.LogError(Component,
                    $"Stored message not found for confirmation | UserId: {message.From!.Id} | MessageId: {message.MessageId}");
                return;
            }

            if (stored.Photos.Count > 0)
            {
                var media = stored.Photos
                    .Select((id, i) => new InputMediaPhoto(id)
                    {
                        Caption = i == 0 ? stored.Text : null
                    })
                    .ToArray();

                await _bot.SendMediaGroup(message.Chat.Id, media);
            }
            else
            {
                var previewText = string.IsNullOrWhiteSpace(stored.Text)
                    ? "📝 (тільки текст без вмісту)"
                    : stored.Text;

                await _bot.SendMessage(
                    message.Chat.Id,
                    previewText,
                    linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
                    parseMode: ParseMode.Html
                );
            }

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Надіслати", $"proceed|{message.From!.Id}|{message.MessageId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Скасувати", $"cancel|{message.From!.Id}|{message.MessageId}")
                }
            });

            await _bot.SendMessage(
                message.Chat.Id,
                "📤 <b>Надіслати цей запит адмінам на перевірку?</b>\n\n",
                replyMarkup: keyboard,
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
                parseMode: ParseMode.Html
            );

            await _logger.LogInfo(Component,
                $"Ask confirmation sent | UserId: {message.From!.Id} | MessageId: {message.MessageId} | Photos: {stored.Photos.Count}");

            session.State = UserState.ConfirmAskContent;
            _sessions.Save(session);
        }
    }
}
