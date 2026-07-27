using System.Text;
using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Handlers
{
    public class HistoryHandler : IAsyncCommandHandler
    {
        private static readonly RequestStatus[] StatusOrder =
        [
            RequestStatus.Pending,
            RequestStatus.Approved,
            RequestStatus.Rejected,
            RequestStatus.Duplicate,
            RequestStatus.Advertisement,
            RequestStatus.NeedsAttention
        ];

        private static readonly Dictionary<RequestStatus, string> StatusLabels = new()
        {
            [RequestStatus.Pending] = "На модерації",
            [RequestStatus.Approved] = "Затверджені",
            [RequestStatus.Rejected] = "Відхилені",
            [RequestStatus.Duplicate] = "Дублікати",
            [RequestStatus.Advertisement] = "Реклама",
            [RequestStatus.NeedsAttention] = "Уточнення"
        };

        private readonly IUserRequestHistoryRepository _history;
        private readonly HistoryOptions _options;
        private readonly TelegramOptions _telegram;
        private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };

        public HistoryHandler(
            IUserRequestHistoryRepository history,
            IOptions<HistoryOptions> options,
            IOptions<TelegramOptions> telegram)
        {
            _history = history;
            _options = options.Value;
            _telegram = telegram.Value;
        }

        public async Task HandleAsync(ITelegramBotClient bot, Message message)
        {
            var userId = message.From!.Id;
            var chatId = message.Chat.Id;
            var isAdmin = userId == _telegram.AdminId;
            var allRequests = await _history.GetByUserId(userId);

            if (!allRequests.Any())
            {
                await bot.SendMessage(
                    chatId,
                    "📭 <b>У вас ще немає історії запитів.</b>\n\n" +
                    "Натисніть кнопку нижче, щоб надіслати свій перший запит 👇",
                    replyMarkup: Keyboards.GetKeyboard(false, isAdmin),
                    linkPreviewOptions: NoPreview,
                    parseMode: ParseMode.Html
                );

                return;
            }

            var counts = await _history.GetStatusCountsByUserIdAsync(userId);
            await bot.SendRichMessage(
                chatId,
                new InputRichMessage { Html = BuildRequestStatsHtml(counts) });

            var maxItems = _options.MaxItemsPerSection;

            var approvedAll = allRequests
                .Where(r => r.Status == RequestStatus.Approved)
                .OrderByDescending(r => r.SubmittedAt)
                .ToList();

            var pendingAll = allRequests
                .Where(r => r.Status == RequestStatus.Pending)
                .OrderByDescending(r => r.SubmittedAt)
                .ToList();

            var approved = approvedAll.Take(maxItems).ToList();
            var pending = pendingAll.Take(maxItems).ToList();

            var markup = Keyboards.GetKeyboard(true, isAdmin);
            var approvedText = "";

            if (approved.Any())
            {
                approvedText = "✅ <b>Затверджені запити:</b>\n\n";
                foreach (var (index, req) in approved.Index())
                {
                    approvedText += $"<b>{index + 1}.</b>\n";
                    if (!string.IsNullOrEmpty(req.ChannelLink))
                    {
                        approvedText += $"🔗 <b>Посилання:</b> {req.ChannelLink}\n";
                    }
                    approvedText += "\n";
                }
                if (approvedAll.Count > maxItems)
                {
                    approvedText += $"… та ще {approvedAll.Count - maxItems} (показано останні {maxItems})\n";
                }
                approvedText = approvedText.TrimEnd();
            }

            var pendingText = "";
            var pendingMessageIds = new List<int>();
            if (pending.Any())
            {
                pendingText = "⏳ <b>Запити на модерації:</b>\n\n";
                foreach (var (index, req) in pending.Index())
                {
                    var itemText = $"<b>{index + 1}.</b>\n";
                    itemText += $"🆔 <b>ID запиту:</b> <code>{req.UserMessageId}</code>\n\n";
                    pendingMessageIds.Add(req.UserMessageId);
                    pendingText += itemText;
                }
                if (pendingAll.Count > maxItems)
                {
                    pendingText += $"… та ще {pendingAll.Count - maxItems} (показано останні {maxItems})\n";
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
                    linkPreviewOptions: NoPreview,
                    parseMode: ParseMode.Html
                );
            }

            if (!string.IsNullOrEmpty(pendingText))
            {
                await bot.SendMessage(
                    chatId,
                    pendingText,
                    replyMarkup: markup,
                    linkPreviewOptions: NoPreview,
                    parseMode: ParseMode.Html
                );
            }

            foreach (var replyId in pendingMessageIds)
            {
                try
                {
                    await bot.SendMessage(
                        chatId: chatId,
                        text: $"⏳ Ваш запит <code>{replyId}</code> все ще <b>очікує модерації</b>.",
                        replyParameters: new ReplyParameters()
                        {
                            MessageId = replyId
                        },
                        linkPreviewOptions: NoPreview,
                        parseMode: ParseMode.Html
                    );
                }
                catch (ApiRequestException ex) when (
                    ex.ErrorCode == 400 &&
                    ex.Message?.Contains("reply message not found") == true)
                {
                    await bot.SendMessage(
                        chatId: chatId,
                        text: $"⏳ Запит <code>{replyId}</code> очікує модерації.\n\n" +
                              "<i>(Оригінальне повідомлення було видалено)</i>",
                        linkPreviewOptions: NoPreview,
                        parseMode: ParseMode.Html
                    );
                }
            }
        }

        private static string BuildRequestStatsHtml(Dictionary<RequestStatus, int> counts)
        {
            var sb = new StringBuilder();
            sb.Append("<b>Статистика запитів</b>");
            sb.Append("<table bordered striped><tr><th>Статус</th><th>Кількість</th></tr>");

            var total = 0;
            foreach (var status in StatusOrder)
            {
                if (!counts.TryGetValue(status, out var count) || count <= 0)
                    continue;

                sb.Append("<tr><td>")
                  .Append(StatusLabels[status])
                  .Append("</td><td>")
                  .Append(count)
                  .Append("</td></tr>");
                total += count;
            }

            sb.Append("<tr><td><b>Всього</b></td><td>")
              .Append("<b>").Append(total).Append("</b>")
              .Append("</td></tr></table>");

            return sb.ToString();
        }
    }
}
