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
            "👋 <b>Напишіть, будь ласка, ваш запит одним повідомленням</b> — так його швидше опрацюють.\n\n" +

            "✍️ <b>Що варто вказати:</b>\n" +
            "• Привітання та суть прохання одразу в перших рядках\n" +
            "• Район / бюджет / дедлайн / інші важливі деталі (якщо є)\n" +
            "• Контакт для зв'язку (за потреби): @username або телефон\n\n" +

            "📌 <b>Приклад:</b>\n" +
            "«Вітаю! Поділіться, будь ласка, контактами хорошого сантехніка у Крихівцях. Писати @username»\n" +
            "Або просто дайте відповідь на цей пост.\n\n" +

            $"🖼️ <b>Фото:</b> можна додати до {_limits.MaxAlbumPhotoCount} зображень одним альбомом " +
            "(для оренди, продажу чи оголошень про загублене — рекомендуємо 2–5 чітких фото).\n\n" +

            "⚠️ <i>Відео, гіфки та інші медіафайли, на жаль, не підтримуються.</i>\n\n" +

            "📜 Повні правила оформлення запиту — /policy\n\n" +

            "Дякуємо, що звертаєтесь до нас! 🙌";
    }
}