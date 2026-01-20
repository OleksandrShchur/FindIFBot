using FindIFBot.Domain;
using FindIFBot.Persistence;
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

            var allRequests = _history.GetByUserId(userId);

            if (!allRequests.Any())
            {
                var initialMarkup = BuildMarkup(userId);
                await bot.SendMessage(
                    chatId,
                    "У вас немає історії запитів.",
                    replyMarkup: initialMarkup
                );
                return;
            }

            var approved = allRequests
                .Where(r => r.Status == Domain.RequestStatus.Approved)
                .OrderByDescending(r => r.SubmittedAt)
                .ToList();

            var pending = allRequests
                .Where(r => r.Status == Domain.RequestStatus.Pending)
                .OrderByDescending(r => r.SubmittedAt)
                .ToList();

            var markup = BuildMarkup(userId);

            string approvedText = "";
            if (approved.Any())
            {
                approvedText = "✅ Затверджені запити:\n\n";
                foreach (var req in approved)
                {
                    approvedText += $"{req.StoredMessage.Text ?? "Без тексту"}\n";
                    if (!string.IsNullOrEmpty(req.ChannelLink))
                    {
                        approvedText += $"Посилання: {req.ChannelLink}\n";
                    }
                    approvedText += "\n";
                }
                approvedText = approvedText.TrimEnd();
            }

            var pendingText = "";
            var pendingMessageToReply = new List<ReplyHistoryMessage>();

            if (pending.Any())
            {
                pendingText = "⏳ Запити на модерації:\n\n";

                foreach (var req in pending)
                {
                    var itemText = $"```{req.StoredMessage.Text ?? "Без тексту"}```\n";
                    itemText += $"ID запиту: {req.UserMessageId}\n";

                    //bool isDeleted = false;
                    //try
                    //{
                    //    var replyMessage = new ReplyHistoryMessage()
                    //    {
                    //        MessageId = (int)req.UserMessageId,
                    //    };

                    //    //await bot.SendMessage(
                    //    //    chatId: chatId,
                    //    //    text: $"⏳ Запит {req.UserMessageId} очікує модерації",
                    //    //    replyParameters: new ReplyParameters()
                    //    //    {
                    //    //        MessageId = (int)req.UserMessageId
                    //    //    }
                    //    //);
                    //}
                    //catch (ApiRequestException ex) when (
                    //    ex.ErrorCode == 400 &&
                    //    ex.Message?.Contains("reply message not found") == true)
                    //{
                    //    isDeleted = true;
                    //}

                    //if (isDeleted)
                    //{
                    //    itemText += "(Оригінальне повідомлення видалено)\n";
                    //}

                    var replyMessage = new ReplyHistoryMessage()
                    {
                        MessageId = (int)req.UserMessageId,
                    };
                    pendingMessageToReply.Add(replyMessage);


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
                    parseMode: ParseMode.MarkdownV2
                );
            }

            if (!string.IsNullOrEmpty(pendingText))
            {
                await bot.SendMessage(
                    chatId,
                    pendingText,
                    replyMarkup: markup,
                    parseMode: ParseMode.MarkdownV2
                );
            }

            foreach (var replyMsg in pendingMessageToReply)
            {
                try
                {
                    await bot.SendMessage(
                        chatId: chatId,
                        text: $"⏳ Запит `{replyMsg.MessageId}` очікує модерації",
                        replyParameters: new ReplyParameters()
                        {
                            MessageId = (int)replyMsg.MessageId
                        },
                        parseMode: ParseMode.MarkdownV2
                    );
                }
                catch (ApiRequestException ex) when (
                    ex.ErrorCode == 400 &&
                    ex.Message?.Contains("reply message not found") == true)
                {
                    await bot.SendMessage(
                        chatId: chatId,
                        text: $"⏳ Запит {replyMsg.MessageId} очікує модерації. (Оригінальне повідомлення видалено).",
                        parseMode: ParseMode.MarkdownV2
                    );
                }
            }
        }

        private ReplyKeyboardMarkup BuildMarkup(long userId)
        {
            var hasHistory = _history.GetByUserId(userId).Any();
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
