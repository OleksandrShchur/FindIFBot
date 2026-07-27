using FindIFBot.Helpers;

namespace FindIFBot.Handlers
{
    public class VersionHandler : ICommandHandler
    {
        public string Handle() =>
            $"📦 <b>Версія бота:</b> {BotVersion.Current}";
    }
}
