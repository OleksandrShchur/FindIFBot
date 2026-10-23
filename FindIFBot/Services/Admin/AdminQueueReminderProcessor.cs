using FindIFBot.Helpers.Logs;
using Microsoft.Extensions.DependencyInjection;

namespace FindIFBot.Services.Admin
{
    /// <summary>
    /// Polls for due admin pending-queue reminders. Safe on MonsterASP when the process
    /// is kept awake by an external monitor.
    /// </summary>
    public class AdminQueueReminderProcessor : BackgroundService
    {
        private const string Component = "AdminQueueReminderProcessor";
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAppLogger<AdminQueueReminderProcessor> _logger;

        public AdminQueueReminderProcessor(
            IServiceScopeFactory scopeFactory,
            IAppLogger<AdminQueueReminderProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var reminder = scope.ServiceProvider.GetRequiredService<IAdminQueueReminderService>();
                    await reminder.ProcessDueAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await _logger.LogError(Component,
                        $"Error while processing admin queue reminders | {ex.GetType().Name}: {ex.Message}");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
