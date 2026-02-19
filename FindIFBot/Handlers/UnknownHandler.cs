namespace FindIFBot.Handlers;

public class UnknownHandler : ICommandHandler
{
    public string Handle() =>
        "❓ <b>Невідома команда.</b>\n\n" +
        "Я не зрозумів вашу команду 😕\n\n" +
        "Ось що я вмію 👇\n\n" +
        new HelpHandler().Handle();
}
