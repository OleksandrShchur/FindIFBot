namespace FindIFBot.Handlers;

public class UnknownHandler : ICommandHandler
{
    private readonly HelpHandler _helpHandler;

    public UnknownHandler(HelpHandler helpHandler)
    {
        _helpHandler = helpHandler;
    }

    public string Handle() =>
        "❓ <b>Невідома команда.</b>\n\n" +
        "Я не зрозумів вашу команду 😕\n\n" +
        "Ось що я вмію 👇\n\n" +
        _helpHandler.Handle();
}
