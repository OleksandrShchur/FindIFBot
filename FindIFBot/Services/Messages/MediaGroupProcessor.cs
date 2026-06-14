using FindIFBot.EF.Repositories;
using FindIFBot.Helpers.Logs;
using Microsoft.Extensions.DependencyInjection;

namespace FindIFBot.Services.Messages
{
    /// <summary>
    /// Hosted background service that drains the <see cref="IMediaGroupQueue"/>. For each work
    /// item it waits the buffering window (so all album messages accumulate), takes the buffered
    /// messages, and processes them inside a fresh DI scope. Replaces the former untracked
    /// <c>Task.Run</c>: exceptions are logged, work is bounded-concurrency, and processing stops
    /// cleanly on host shutdown. A per-item timeout prevents a single stuck send from blocking work.
    /// </summary>
    public class MediaGroupProcessor : BackgroundService
    {
        private const string Component = "MediaGroupProcessor";
        private static readonly TimeSpan BufferDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(2);
        private const int MaxDegreeOfParallelism = 4;

        private readonly IMediaGroupQueue _queue;
        private readonly IMediaGroupBuffer _buffer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAppLogger<MediaGroupProcessor> _logger;

        public MediaGroupProcessor(
            IMediaGroupQueue queue,
            IMediaGroupBuffer buffer,
            IServiceScopeFactory scopeFactory,
            IAppLogger<MediaGroupProcessor> logger)
        {
            _queue = queue;
            _buffer = buffer;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                CancellationToken = stoppingToken
            };

            try
            {
                await Parallel.ForEachAsync(
                    _queue.Reader.ReadAllAsync(stoppingToken),
                    options,
                    async (item, ct) => await ProcessItemAsync(item, ct));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown.
            }
        }

        private async Task ProcessItemAsync(MediaGroupWorkItem item, CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(BufferDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!_buffer.TryTake(item.UserId, item.MediaGroupId, out var group))
            {
                await _logger.LogWarning(Component,
                    $"Media group buffer empty or removed | user={item.UserId} | groupId={item.MediaGroupId}");
                return;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutCts.CancelAfter(ProcessingTimeout);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
                var history = scope.ServiceProvider.GetRequiredService<IUserRequestHistoryRepository>();
                var handler = scope.ServiceProvider.GetRequiredService<IMediaGroupHandler>();

                var currentSession = await sessions.GetAsync(item.UserId, timeoutCts.Token);

                await _logger.LogInfo(Component,
                    $"Processing media group | user={item.UserId} | state={currentSession.State} | photos={group.Count}");

                await handler.ProcessAsync(group, currentSession, history);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
            {
                await _logger.LogError(Component,
                    $"Media group processing timed out after {ProcessingTimeout.TotalSeconds}s | user={item.UserId} | groupId={item.MediaGroupId}");
            }
            catch (Exception ex)
            {
                await _logger.LogError(Component,
                    $"Media group processing failed | user={item.UserId} | ex={ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
