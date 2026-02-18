namespace FindIFBot.Handlers;

public class UnknownHandler : ICommandHandler
{
    private const string UnknownCommandMessage =
        "Невідома команда. Спробуйте ввести доступні функції.";

    public string Handle() =>
        $"{UnknownCommandMessage}\n\n{new HelpHandler().Handle()}";
}
