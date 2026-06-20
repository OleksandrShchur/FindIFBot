using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.Persistence;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FindIFBot.Services.Admin
{
    public class AdminRequestNotifier : IAdminRequestNotifier
    {
        private readonly ITelegramBotClient _bot;
        private readonly TelegramOptions _options;
        private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };

        public AdminRequestNotifier(
            ITelegramBotClient bot,
            IOptions<TelegramOptions> options)
        {
            _bot = bot;
            _options = options.Value;
        }

        public async Task SendToAdminAsync(StoredMessage stored, UserInfo userInfo)
        {
            await _bot.SendMessage(
                _options.AdminId,
                $"Інформація про користувача:" +
                $"\n\n<b>ID:</b> {userInfo.Id}" +
                $"\n<b>UserName:</b> {(string.IsNullOrEmpty(userInfo.UserName) ? "—" : $"@{userInfo.UserName}")}" +
                $"\n<b>First Name:</b> {Format(userInfo.FirstName)}" +
                $"\n<b>Last Name:</b> {Format(userInfo.LastName)}" +
                $"\n<b>Language Code:</b> {Format(userInfo.LanguageCode)}" +
                $"\n<b>Is Bot:</b> {(userInfo.IsBot ? "✅ Так" : "❌ Ні")}" +
                $"\n<b>Is Premium:</b> {(userInfo.IsPremium ? "✅ Так" : "❌ Ні")}",
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html);

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Схвалити пост", $"+ask|{stored.UserId}|{stored.MessageId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Відхилити пост", $"-ask|{stored.UserId}|{stored.MessageId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📋 Дублікат посту", $"?ask|{stored.UserId}|{stored.MessageId}")
                }
            });

            if (stored.Photos.Count > 0)
            {
                var media = stored.Photos
                    .Select((id, i) => new InputMediaPhoto(id) { Caption = i == 0 ? stored.Text : null })
                    .ToArray();

                await _bot.SendMediaGroup(_options.AdminId, media);
                await _bot.SendMessage(
                    _options.AdminId, 
                    "🛠 Дії модерації:", 
                    linkPreviewOptions: NoPreview,
                    replyMarkup: keyboard
                );
            }
            else
            {
                await _bot.SendMessage(
                    _options.AdminId, 
                    stored.Text ?? "📝 (тільки текст без вмісту)", 
                    linkPreviewOptions: NoPreview,
                    replyMarkup: keyboard
                );
            }
        }

        private static string Format(string value) =>
            string.IsNullOrEmpty(value) ? "—" : value;
    }
}
