using FindIFBot.Configuration;
using FindIFBot.Domain;
using FindIFBot.EF;
using FindIFBot.EF.Entities;
using FindIFBot.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly ILogger<MaintenanceService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ITelegramBotClient _botClient;
        private readonly TelegramOptions _telegramOptions;
        private readonly BotDbContext _dbContext;
        private readonly TimeProvider _timeProvider;
        private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };

        public MaintenanceService(ILogger<MaintenanceService> logger,
            IWebHostEnvironment environment,
            ITelegramBotClient botClient,
            IOptions<TelegramOptions> telegramOptions,
            BotDbContext dbContext,
            TimeProvider? timeProvider = null)
        {
            _logger = logger;
            _environment = environment;
            _botClient = botClient;
            _telegramOptions = telegramOptions.Value;
            _dbContext = dbContext;
            _timeProvider = timeProvider ?? TimeProvider.System;
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
            var kyivDate = KyivWorkingHours.GetKyivDate(_timeProvider);
            var windowEndUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var windowStartUtc = windowEndUtc.AddHours(-24);

            var botUserCount = await _dbContext.UserSessions
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var channelSubscriberCount = await _botClient.GetChatMemberCount(
                _telegramOptions.UserOutputChannel,
                cancellationToken);

            var postsCount = await _dbContext.UserRequests
                .AsNoTracking()
                .CountAsync(r =>
                    r.Status == RequestStatus.Approved
                    && r.PublishedAtUtc != null
                    && r.PublishedAtUtc >= windowStartUtc
                    && r.PublishedAtUtc < windowEndUtc,
                    cancellationToken);

            await UpsertDailyStatisticAsync(
                kyivDate,
                botUserCount,
                channelSubscriberCount,
                postsCount,
                cancellationToken);

            var message = BuildStatisticsMessage(kyivDate, botUserCount, channelSubscriberCount, postsCount);

            await _botClient.SendMessage(
                chatId: _telegramOptions.LogsOutputChannel,
                messageThreadId: _telegramOptions.LogsThreadId,
                text: message,
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }

        private async Task UpsertDailyStatisticAsync(
            DateOnly kyivDate,
            int botUserCount,
            int channelSubscriberCount,
            int postsCount,
            CancellationToken cancellationToken)
        {
            var existing = await _dbContext.ChannelDailyStatistics
                .FirstOrDefaultAsync(s => s.Date == kyivDate, cancellationToken);

            if (existing is null)
            {
                _dbContext.ChannelDailyStatistics.Add(new ChannelDailyStatistic
                {
                    Date = kyivDate,
                    BotUserCount = botUserCount,
                    ChannelSubscriberCount = channelSubscriberCount,
                    PostsCount = postsCount,
                    CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
                });
            }
            else
            {
                existing.BotUserCount = botUserCount;
                existing.ChannelSubscriberCount = channelSubscriberCount;
                existing.PostsCount = postsCount;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static string BuildStatisticsMessage(
            DateOnly kyivDate,
            int botUserCount,
            int channelSubscriberCount,
            int postsCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📊 Daily Statistics — {kyivDate:dd.MM.yyyy}");
            sb.AppendLine();
            sb.Append("<pre>");
            sb.AppendLine("Metric                  Value");
            sb.AppendLine("----------------------  ------");
            sb.AppendLine($"Bot users               {botUserCount.ToString("N0", CultureInfo.InvariantCulture),6}");
            sb.AppendLine($"Channel subscribers     {channelSubscriberCount.ToString("N0", CultureInfo.InvariantCulture),6}");
            sb.AppendLine($"Posts last 24h          {postsCount.ToString("N0", CultureInfo.InvariantCulture),6}");
            sb.Append("</pre>");
            return sb.ToString();
        }
    }
}
