using FindIFBot.Configuration;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Handlers
{
    public class AdminPendingHandler : IAsyncCommandHandler
    {
        private readonly IUserRequestHistoryRepository _history;
        private readonly HistoryOptions _historyOptions;
        private readonly TelegramOptions _telegram;
        private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };

        public AdminPendingHandler(
            IUserRequestHistoryRepository history,
            IOptions<HistoryOptions> historyOptions,
            IOptions<TelegramOptions> telegram)
        {
            _history = history;
            _historyOptions = historyOptions.Value;
            _telegram = telegram.Value;
        }

        public async Task HandleAsync(ITelegramBotClient bot, Message message)
        {
            var userId = message.From!.Id;
            var chatId = message.Chat.Id;

            if (userId != _telegram.AdminId)
            {
                await bot.SendMessage(
                    chatId,
                    "⛔️ Ця команда доступна лише адміністратору.",
                    replyMarkup: Keyboards.GetKeyboard(await _history.HasHistory(userId), isAdmin: false),
                    linkPreviewOptions: NoPreview,
                    parseMode: ParseMode.Html);
                return;
            }

            var pending = await _history.GetPendingAsync(_historyOptions.MaxItemsPerSection);
            var markup = Keyboards.GetKeyboard(await _history.HasHistory(userId), isAdmin: true);

            if (pending.Count == 0)
            {
                await bot.SendMessage(
                    chatId,
                    "✅ <b>Черга модерації порожня.</b>\n\nНемає запитів, що очікують дії адміна.",
                    replyMarkup: markup,
                    linkPreviewOptions: NoPreview,
                    parseMode: ParseMode.Html);
                return;
            }

            await bot.SendMessage(
                chatId,
                $"⏳ <b>Запити в черзі:</b> {pending.Count}" +
                (pending.Count >= _historyOptions.MaxItemsPerSection
                    ? $" (показано останні {_historyOptions.MaxItemsPerSection})"
                    : string.Empty),
                replyMarkup: markup,
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html);

            foreach (var request in pending)
            {
                var text = $"⏳ Запит #<code>{request.UserMessageId}</code> очікує дії адміна.";

                if (request.AdminInfoMessageId is int adminInfoMessageId)
                {
                    try
                    {
                        await bot.SendMessage(
                            chatId: chatId,
                            text: text,
                            replyParameters: new ReplyParameters { MessageId = adminInfoMessageId },
                            linkPreviewOptions: NoPreview,
                            parseMode: ParseMode.Html);
                        continue;
                    }
                    catch (ApiRequestException ex) when (
                        ex.ErrorCode == 400 &&
                        ex.Message?.Contains("reply message not found") == true)
                    {
                        await bot.SendMessage(
                            chatId: chatId,
                            text: text + "\n\n<i>(Оригінальне повідомлення з інформацією про користувача не знайдено)</i>",
                            linkPreviewOptions: NoPreview,
                            parseMode: ParseMode.Html);
                        continue;
                    }
                }

                await bot.SendMessage(
                    chatId: chatId,
                    text: text + "\n\n<i>(Посилання на тред модерації відсутнє для цього запиту)</i>",
                    linkPreviewOptions: NoPreview,
                    parseMode: ParseMode.Html);
            }
        }
    }
}
