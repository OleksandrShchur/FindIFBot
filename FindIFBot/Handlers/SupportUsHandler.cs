using FindIFBot.Configuration;
using Microsoft.Extensions.Options;

namespace FindIFBot.Handlers
{
    public class SupportUsHandler : ICommandHandler
    {
        private readonly TelegramOptions _options;

        public SupportUsHandler(IOptions<TelegramOptions> options)
        {
            _options = options.Value;
        }

        public string Handle() =>
            "❤️ <b>Підтримати наш проєкт</b>\n\n" +
            "Дякуємо, що хочете допомогти нам розвивати бота та підтримувати канал! 🙏\n" +
            "Кожна ваша гривня дуже важлива для нас ❤️\n\n" +
            "🔗 <b>Посилання на банку:</b>\n" +
            $"{_options.BankLink}\n\n" +
            "💳 <b>Номер картки:</b>\n" +
            $"<code>{_options.CardNumber}</code>\n\n" +
            "З будь-якою сумою — будемо щиро вдячні! 💙";
    }
}
