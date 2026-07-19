using FindIFBot.Configuration;
using FindIFBot.Helpers;
using FindIFBot.Persistence;
using FindIFBot.Utils;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Services.Admin
{
    public class RequestPublisher : IRequestPublisher
    {
        private readonly ITelegramBotClient _bot;
        private readonly TelegramOptions _options;
        private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };

        public RequestPublisher(
            ITelegramBotClient bot,
            IOptions<TelegramOptions> options)
        {
            _bot = bot;
            _options = options.Value;
        }

        public async Task<string> PublishAsync(StoredMessage stored)
        {
            var bodyHtml = MessageEntityHtml.Format(stored.Text, stored.TextEntities);
            var postText = PostTemplate.Build(bodyHtml, _options);
            var postId = 0;

            if (stored.Photos.Count == 1)
            {
                var result = await _bot.SendPhoto(
                    _options.UserOutputChannel,
                    stored.Photos[0],
                    caption: postText,
                    parseMode: ParseMode.Html);
                postId = result.MessageId;
            }
            else if (stored.Photos.Count > 1)
            {
                var media = stored.Photos
                    .Select((id, i) => new InputMediaPhoto(id)
                    {
                        Caption = i == 0 ? postText : null,
                        ParseMode = ParseMode.Html
                    })
                    .ToArray();

                var result = await _bot.SendMediaGroup(_options.UserOutputChannel, media);
                postId = result.First().MessageId;
            }
            else
            {
                var result = await _bot.SendMessage(
                    _options.UserOutputChannel,
                    postText,
                    linkPreviewOptions: NoPreview,
                    parseMode: ParseMode.Html);
                postId = result.MessageId;
            }

            return $"{_options.LinkToChannel}/{postId}";
        }
    }
}
