using System.Text;
using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF.Repositories;
using FindIFBot.Persistence;
using FindIFBot.Utils;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Services.Admin
{
    public class AdminRequestNotifier : IAdminRequestNotifier
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

        private readonly ITelegramBotClient _bot;
        private readonly TelegramOptions _options;
        private readonly IUserRequestHistoryRepository _history;
        private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };

        public AdminRequestNotifier(
            ITelegramBotClient bot,
            IOptions<TelegramOptions> options,
            IUserRequestHistoryRepository history)
        {
            _bot = bot;
            _options = options.Value;
            _history = history;
        }

        public async Task<int> SendToAdminAsync(StoredMessage stored, UserInfo userInfo)
        {
            var infoHtml = BuildUserInfoHtml(stored, userInfo)
                           + "\n\n"
                           + await BuildRequestStatsHtmlAsync(stored.UserId);

            var infoMessage = await _bot.SendRichMessage(
                _options.AdminId,
                new InputRichMessage { Html = infoHtml });

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Схвалити пост", $"+ask|{stored.UserId}|{stored.MessageId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Відхилити пост", $"-ask|{stored.UserId}|{stored.MessageId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📋 Дублікат посту", $"?ask|{stored.UserId}|{stored.MessageId}"),
                    InlineKeyboardButton.WithCallbackData("📣 Реклама", $"!ask|{stored.UserId}|{stored.MessageId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("💬 Уточнити", $"*ask|{stored.UserId}|{stored.MessageId}")
                }
            });

            if (stored.Photos.Count > 0)
            {
                var captionHtml = FormatBodyOrNull(stored);
                var media = stored.Photos
                    .Select((id, i) => new InputMediaPhoto(id)
                    {
                        Caption = i == 0 ? captionHtml : null,
                        ParseMode = i == 0 && captionHtml != null ? ParseMode.Html : default
                    })
                    .ToArray();

                await _bot.SendMediaGroup(_options.AdminId, media);
            }
            else
            {
                var body = string.IsNullOrWhiteSpace(stored.Text)
                    ? "📝 (тільки текст без вмісту)"
                    : MessageEntityHtml.Format(stored.Text, stored.TextEntities);

                await _bot.SendMessage(
                    _options.AdminId,
                    body,
                    linkPreviewOptions: NoPreview,
                    parseMode: ParseMode.Html
                );
            }

            await _bot.SendMessage(
                _options.AdminId,
                $"Дії модерації до #<code>{stored.MessageId}</code>",
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );

            return infoMessage.MessageId;
        }

        private static string BuildUserInfoHtml(StoredMessage stored, UserInfo userInfo) =>
            $"🆔 <b>ID запиту:</b> #<code>{stored.MessageId}</code>" +
            $"\n\nІнформація про користувача:" +
            $"\n\n<b>ID:</b> {userInfo.Id}" +
            $"\n<b>UserName:</b> {(string.IsNullOrEmpty(userInfo.UserName) ? "—" : $"@{userInfo.UserName}")}" +
            $"\n<b>First Name:</b> {Format(userInfo.FirstName)}" +
            $"\n<b>Last Name:</b> {Format(userInfo.LastName)}" +
            $"\n<b>Language Code:</b> {Format(userInfo.LanguageCode)}" +
            $"\n<b>Is Bot:</b> {(userInfo.IsBot ? "✅ Так" : "❌ Ні")}" +
            $"\n<b>Is Premium:</b> {(userInfo.IsPremium ? "✅ Так" : "❌ Ні")}";

        private async Task<string> BuildRequestStatsHtmlAsync(long userId)
        {
            var counts = await _history.GetStatusCountsByUserIdAsync(userId);

            // Current request is persisted after this message is sent.
            counts.TryGetValue(RequestStatus.Pending, out var pending);
            counts[RequestStatus.Pending] = pending + 1;

            var sb = new StringBuilder();
            sb.Append("<h3>User Requests Statistic</h3>");
            sb.Append("<table><tr><th>Status</th><th>Count</th></tr>");

            var total = 0;
            foreach (var status in StatusOrder)
            {
                if (!counts.TryGetValue(status, out var count) || count <= 0)
                    continue;

                sb.Append("<tr><td>")
                  .Append(status)
                  .Append("</td><td>")
                  .Append(count)
                  .Append("</td></tr>");
                total += count;
            }

            sb.Append("<tr><td>Total</td><td>")
              .Append(total)
              .Append("</td></tr></table>");

            return sb.ToString();
        }

        private static string? FormatBodyOrNull(StoredMessage stored) =>
            string.IsNullOrWhiteSpace(stored.Text)
                ? null
                : MessageEntityHtml.Format(stored.Text, stored.TextEntities);

        private static string Format(string value) =>
            string.IsNullOrEmpty(value) ? "—" : value;
    }
}
