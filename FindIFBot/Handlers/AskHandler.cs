namespace FindIFBot.Handlers
{
    public class AskHandler : ICommandHandler
    {
        public string Handle() => 
            "👋 <b>Будь ласка, напишіть ваш запит в одному повідомленні.</b>\n\n" +
            "🖼️ За потреби можете прикріпити <b>до 10 зображень</b>.\n\n" +
            "🤖 Наш бот одразу його опрацює!\n\n" +
            "⚠️ Відео, гіфки та інші медіафайли <b>не обробляються</b>.";
    }
}
