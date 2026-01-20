using FindIFBot.Domain;
using FindIFBot.Persistence;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
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
            var requests = _history.GetByUserId(userId);

            if (!requests.Any())
            {
                var initialMarkup = BuildMarkup(userId);
                await bot.SendMessage(
                    message.Chat.Id,
                    "У вас немає історії запитів.",
                    replyMarkup: initialMarkup
                );
                return;
            }

            var approved = requests.Where(r => r.Status == Domain.RequestStatus.Approved).OrderByDescending(r => r.SubmittedAt).ToList();
            var pending = requests.Where(r => r.Status == Domain.RequestStatus.Pending).OrderByDescending(r => r.SubmittedAt).ToList();

            var text = "";

            if (approved.Any())
            {
                text += "✅ Затверджені запити:\n";
                foreach (var req in approved)
                {
                    text += $"{req.StoredMessage.Text ?? "Без тексту"}\n";
                    if (!string.IsNullOrEmpty(req.ChannelLink))
                    {
                        text += $"Посилання: {req.ChannelLink}\n";
                    }
                    text += "\n";
                }
            }

            if (pending.Any())
            {
                text += "⏳ Запити на модерації:\n";
                foreach (var req in pending)
                {
                    text += $"{req.StoredMessage.Text ?? "Без тексту"}\n";
                    // For pending, perhaps show message id or something
                    text += $"ID повідомлення: {req.UserMessageId}\n\n";
                }
            }

            var markup = BuildMarkup(userId);
            await bot.SendMessage(
                message.Chat.Id,
                text.Trim(),
                replyMarkup: markup
            );
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