namespace FindIFBot.Configuration
{
    public sealed class TelegramOptions
    {
        public string BotToken { get; init; } = string.Empty;
        public string WebhookUrl { get; init; } = string.Empty;
        public long AdminId { get; init; }
        public string UserOutputChannel { get; init; } = string.Empty;
        public string LinkToChannel { get; init; } = string.Empty;
        public string ChatInviteLink { get; init; } = string.Empty;
        public string BotUsername { get; init; } = string.Empty;
        public string LogsOutputChannel { get; init; } = string.Empty;
        public int LogsThreadId { get; init; }
        public int ErrorLogsThreadId { get; init; }
        public int AllMessagesThreadId { get; init; }
        public int RetryMaxAttempts { get; init; }
        public string BankLink { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
    }
}
