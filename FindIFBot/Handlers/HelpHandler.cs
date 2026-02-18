namespace FindIFBot.Handlers
{
    public class HelpHandler : ICommandHandler
    {
        public string Handle() => "Що вміє наш бот:\n" +
            "/ask - ввести запит та відправити запит на публікацію в канал\n" +
            "/history - отримати перелік запитів, що були опубліковані або перебувають у стані модерації нами.\n";
    }
}
