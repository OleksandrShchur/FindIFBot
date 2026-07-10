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

            "✍️ <b>Порада:</b>\n" +
            "• Перші рядки — привітання та чітке прохання\n" +
            "• Район / бюджет / дедлайн / обмеження (якщо є)\n" +
            "• Контакт: @username або телефон за потреби\n" +
            "• Одне прохання = один пост\n\n" +

            "📌 <b>Приклад:</b>\n" +
            "«Вітаю! Підкажіть сантехніка у Крихівцях. Бажано сьогодні-завтра. Бюджет до … грн. Пишіть @username»\n\n" +

            $"🖼️ Можете прикріпити <b>до {_limits.MaxAlbumPhotoCount} зображень</b> " +
            "(для оренди/продажу чи загубленого краще 2–5 чітких фото).\n\n" +

            "📞 За потреби залиште <b>ваші контакти</b> для зворотнього зв'язку.\n\n" +

            "⚠️ Відео, гіфки та інші медіафайли <b>не обробляються</b>.\n\n" +

            "📜 Детальні правила: /policy";
    }
}
