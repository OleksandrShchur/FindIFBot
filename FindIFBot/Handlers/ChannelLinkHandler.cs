using FindIFBot.Configuration;
using Microsoft.Extensions.Options;

namespace FindIFBot.Handlers
{
    public class ChannelLinkHandler : ICommandHandler
    {
        private readonly TelegramOptions _options;

        public ChannelLinkHandler(IOptions<TelegramOptions> options)
        {
            _options = options.Value;
        }

        public string Handle() =>
            "📢 <b>Франківськ Питає — канал</b>\n\n" +
            "Тут зібрані корисні запити та важливі оголошення для мешканців міста.\n" +
            "Приєднуйтесь, щоб залишатися в курсі та знаходити потрібне швидше.\n\n" +
            $"👉 <a href=\"{_options.LinkToChannel}\">Приєднатись до каналу</a>";
    }
}
