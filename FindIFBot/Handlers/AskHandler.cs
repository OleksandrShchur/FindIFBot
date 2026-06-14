using FindIFBot.Configuration;
using Microsoft.Extensions.Options;

namespace FindIFBot.Handlers
{
    public class AskHandler : ICommandHandler
    {
        private readonly SubmissionOptions _limits;

        public AskHandler(IOptions<SubmissionOptions> limits)
        {
            _limits = limits.Value;
        }

        public string Handle() =>
            "👋 <b>Будь ласка, напишіть ваш запит в одному повідомленні.</b>\n\n" +
            $"🖼️ Можете прикріпити <b>до {_limits.MaxAlbumPhotoCount} зображень</b>.\n\n" +
            "📞 За потреби залиште <b>ваші контакти</b> для зворотнього зв'язку.\n\n" +
            "🤖 Наш бот одразу його опрацює!\n\n" +
            "⚠️ Відео, гіфки та інші медіафайли <b>не обробляються</b>.";
    }
}
