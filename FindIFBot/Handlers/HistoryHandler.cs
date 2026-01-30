using FindIFBot.Domain;
using FindIFBot.EF.Repositories;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Handlers
{
    public class HistoryHandler : IHistoryHandler
    {
        private readonly IUserRequestHistoryRepository _history;
        public HistoryHandler(IUserRequestHistoryRepository history)
        {
            _history = history;
        }

        public async Task HandleAsync(ITelegramBotClient bot, Message message)
        {
            var userId = message.From!.Id;
            var chatId = message.Chat.Id;

            var allRequests = await _history.GetByUserId(userId);

            if (!allRequests.Any())
            {
                var initialMarkup = await BuildMarkup(userId);
                await bot.SendMessage(
                    chatId,
                    "У вас немає історії запитів.",
                    replyMarkup: initialMarkup
                );
                return;
            }

            var approved = allRequests
                .Where(r => r.Status == RequestStatus.Approved)
                .OrderByDescending(r => r.SubmittedAt)
                .ToList();

            var pending = allRequests
                .Where(r => r.Status == RequestStatus.Pending)
                .OrderByDescending(r => r.SubmittedAt)
                .ToList();

            var markup = await BuildMarkup(userId);

            string approvedText = "";
            if (approved.Any())
            {
                approvedText = "✅ Затверджені запити:\n\n";
                foreach (var (index, req) in approved.Index())
                {
                    approvedText += $"<code>- {index + 1}</code>\n";
                    if (!string.IsNullOrEmpty(req.ChannelLink))
                    {
                        approvedText += $"Посилання: {req.ChannelLink}\n";
                    }
                    approvedText += "\n";
                }
                approvedText = approvedText.TrimEnd();
            }

            var pendingText = "";
            var pendingMessageIds = new List<int>();

            if (pending.Any())
            {
                pendingText = "⏳ Запити на модерації:\n\n";

                foreach (var (index, req) in pending.Index())
                {
                    var itemText = $"<code>- {index + 1}</code>\n";

                    itemText += $"ID запиту: {req.UserMessageId}\n";
                    pendingMessageIds.Add(req.UserMessageId);

                    itemText += "\n";
                    pendingText += itemText;
                }

                pendingText = pendingText.TrimEnd();
            }

            if (!string.IsNullOrEmpty(approvedText))
            {
                var replyMarkupForApproved = string.IsNullOrEmpty(pendingText) ? markup : null;
                await bot.SendMessage(
                    chatId,
                    approvedText,
                    replyMarkup: replyMarkupForApproved,
                    parseMode: ParseMode.Html
                );
            }

            if (!string.IsNullOrEmpty(pendingText))
            {
                await bot.SendMessage(
                    chatId,
                    pendingText,
                    replyMarkup: markup,
                    parseMode: ParseMode.Html
                );
            }

            foreach (var replyId in pendingMessageIds)
            {
                try
                {
                    await bot.SendMessage(
                        chatId: chatId,
                        text: $"⏳ Запит <code>{replyId}</code> очікує модерації",
                        replyParameters: new ReplyParameters()
                        {
                            MessageId = replyId
                        },
                        parseMode: ParseMode.Html
                    );
                }
                catch (ApiRequestException ex) when (
                    ex.ErrorCode == 400 &&
                    ex.Message?.Contains("reply message not found") == true)
                {
                    await bot.SendMessage(
                        chatId: chatId,
                        text: $"⏳ Запит <code>{replyId}</code> очікує модерації. (Оригінальне повідомлення видалено).",
                        parseMode: ParseMode.Html
                    );
                }
            }
        }

        private async Task<ReplyKeyboardMarkup> BuildMarkup(long userId)
        {
            var hasHistory = (await _history.GetByUserId(userId)).Any();
            var keyboard = new List<KeyboardButton[]>
            {
                new KeyboardButton[] { "Розпочати пошук" }
            };
            if (hasHistory)
            {
                keyboard.Add(new KeyboardButton[] { "Історія запитів" });
            }
            keyboard.Add(new KeyboardButton[] { "Довідка" });

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }
    }
}
