using FindIFBot.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Helpers.Logs;

public class AppLogger<T> : IAppLogger<T>
{
    private readonly TelegramOptions _options;
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<T> _logger;
    private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };

    public AppLogger(ITelegramBotClient bot, IOptions<TelegramOptions> options, ILogger<T> logger)
    {
        _bot = bot;
        _options = options.Value;
        _logger = logger;
    }

    public Task LogInfo(string component, string message) => Log(component, message, LogLevel.Information);

    public Task LogWarning(string component, string message) => Log(component, message, LogLevel.Warning);

    public Task LogError(string component, string message) => Log(component, message, LogLevel.Error);

    private async Task Log(string component, string message, LogLevel level)
    {
        var line = FormatLine(component, message, level);

        _logger.Log(level, message);

        if (level == LogLevel.Error)
        {
            await SendWithRetry(line, _options.ErrorLogsThreadId, _options.RetryMaxAttempts);
        }

        //var threadIds = level == LogLevel.Error
        //    ? new[] { _options.LogsThreadId, _options.ErrorLogsThreadId }
        //    : new[] { _options.LogsThreadId };

        //foreach (var threadId in threadIds)
        //{
        //    await SendWithRetry(line, threadId, _options.RetryMaxAttempts);
        //}
    }

    private string FormatLine(string component, string message, LogLevel level)
        => $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{component}] - [{level.ToString().ToUpper()}]: {message}";

    private async Task SendWithRetry(string text, int? threadId, int maxAttempts = 3)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _bot.SendMessage(
                    chatId: _options.LogsOutputChannel,
                    text: text,
                    linkPreviewOptions: NoPreview,
                    parseMode: ParseMode.Html,
                    messageThreadId: threadId);

                return;
            }
            catch (ApiRequestException ex)
            {
                _logger.LogError(ex, "Telegram API error while sending log. Attempt {Attempt}", attempt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending log. Attempt {Attempt}", attempt);
            }

            if (attempt < maxAttempts)
                await Task.Delay(500);
        }
    }
}
