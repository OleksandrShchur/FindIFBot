using FindIFBot.Configuration;
using FindIFBot.Helpers;
using FindIFBot.Persistence;
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

        public RequestPublisher(
            ITelegramBotClient bot,
            IOptions<TelegramOptions> options)
        {
            _bot = bot;
            _options = options.Value;
        }

        public async Task<string> PublishAsync(StoredMessage stored)
        {
            var postText = PostTemplate.Build(stored.Text, _options);
            var postId = 0;

            if (stored.Photos.Count > 0)
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
                    linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true },
                    parseMode: ParseMode.Html
                );
                postId = result.MessageId;
            }

            return $"{_options.LinkToChannel}/{postId}";
        }
    }
}
