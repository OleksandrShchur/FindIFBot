namespace FindIFBot.Configuration
{
    public sealed class TelegramOptions
    {
        public string BotToken { get; init; } = string.Empty;
        public string WebhookUrl { get; init; } = string.Empty;
        public long AdminId { get; init; }
        public string UserOutputChannel { get; init; } = string.Empty;
        public string LinkToChannel { get; init; } = string.Empty;
        public string LogsOutputChannel { get; init; } = string.Empty;
        public long LogsThreadId { get; init; }
        public long ErrorLogsThreadId { get; init; }
        public long AllMessagesOutputChannel { get; init; }
    }
}
