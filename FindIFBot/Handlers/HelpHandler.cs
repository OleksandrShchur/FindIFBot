namespace FindIFBot.Handlers
{
    public class HelpHandler : ICommandHandler
    {
        public string Handle() =>
            "🤖 <b>Що я вмію:</b>\n\n" +
            "📨 <b>/ask</b> — надіслати запит на публікацію в канал\n\n" +
            "📋 <b>/history</b> — переглянути історію ваших запитів (опубліковані та на модерації)\n\n" +
            "📜 <b>/policy</b> — опис наших правил\n\n" +
            "❤️ <b>/support</b> — підтримати наш проєкт\n\n" +
            "ℹ️ <b>/help</b> — показати цю довідку\n\n" +
            "🔗 <b>/channel</b> — поcилання на канал\n\n" +
            "\nПросто виберіть потрібну команду 👇";
    }
}
