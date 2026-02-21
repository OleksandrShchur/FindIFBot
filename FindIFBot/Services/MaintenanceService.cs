using FindIFBot.Configuration;
using FindIFBot.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FindIFBot.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly ILogger<MaintenanceService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ITelegramBotClient _botClient;
        private readonly TelegramOptions _telegramOptions;
        private readonly BotDbContext _dbContext;

        public MaintenanceService(ILogger<MaintenanceService> logger,
            IWebHostEnvironment environment,
            ITelegramBotClient botClient,
            IOptions<TelegramOptions> telegramOptions,
            BotDbContext dbContext)
        {
            _logger = logger;
            _environment = environment;
            _botClient = botClient;
            _telegramOptions = telegramOptions.Value;
            _dbContext = dbContext;
        }

        public async Task ProcessYesterdayLogsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var logsFolder = Path.Combine(_environment.ContentRootPath, "logs");
                if (!Directory.Exists(logsFolder))
                {
                    _logger.LogWarning("Logs folder not found at {Path}", logsFolder);

                    return;
                }

                // previous day using local server time
                var yesterday = DateTime.Today.AddDays(-1);
                var yesterdayStr = yesterday.ToString("yyyyMMdd");

                // find ALL log files belonging to yesterday
                var logFiles = Directory.EnumerateFiles(logsFolder, "log-*.txt", SearchOption.TopDirectoryOnly)
                    .Where(file =>
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        return fileName.StartsWith("log-") &&
                               fileName.Length >= 12 &&
                               fileName.Substring(4, 8) == yesterdayStr;
                    })
                    .ToList();

                if (logFiles.Count == 0)
                {
                    _logger.LogInformation("No log files found for {Date}", yesterday.ToString("yyyy-MM-dd"));

                    return;
                }

                _logger.LogInformation("Found {Count} log file(s) for {Date}", logFiles.Count, yesterday.ToString("yyyy-MM-dd"));

                foreach (var filePath in logFiles)
                {
                    var fileName = Path.GetFileName(filePath);

                    try
                    {
                        await using var fileStream = File.OpenRead(filePath);

                        var inputFile = InputFile.FromStream(fileStream, fileName);

                        await _botClient.SendDocument(
                            chatId: _telegramOptions.LogsOutputChannel,
                            messageThreadId: _telegramOptions.LogsThreadId,
                            document: inputFile,
                            caption: $"📋 Daily logs — {yesterday:dd.MM.yyyy}\nFile: {fileName}",
                            cancellationToken: cancellationToken);

                        // Close stream before delete
                        await fileStream.DisposeAsync();

                        File.Delete(filePath);
                        _logger.LogInformation("Sent and deleted: {File}", fileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send/delete {File}", fileName);
                        // Do NOT delete if send failed
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in daily log maintenance");
            }
        }

        public async Task SendDailyStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var userCount = await _dbContext.UserSessions
                .AsNoTracking()
                .CountAsync(cancellationToken);

            await _botClient.SendMessage(
                chatId: _telegramOptions.LogsOutputChannel,
                messageThreadId: _telegramOptions.LogsThreadId,
                text: $"📊 Daily Statistics — {DateTime.Today:dd.MM.yyyy}\n👥 Total users: {userCount:N0}",
                cancellationToken: cancellationToken);
        }
    }
}
