using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers.Logs;
using FindIFBot.Services.Ask;
using FindIFBot.Services.Messages;
using FindIFBot.UnitTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;
using Telegram.Bot.Types;

namespace FindIFBot.UnitTests.Services.Messages
{
    public class MediaGroupProcessorTests
    {
        private const long UserId = 42;

        [Fact]
        public async Task ProcessItem_WhenHandlerThrows_NotifiesUnexpectedError()
        {
            var queue = new TestMediaGroupQueue();
            var buffer = Substitute.For<IMediaGroupBuffer>();
            var handler = Substitute.For<IMediaGroupHandler>();
            var sessions = Substitute.For<IUserSessionRepository>();
            var history = Substitute.For<IUserRequestHistoryRepository>();
            var unexpectedError = Substitute.For<IAskUnexpectedErrorNotifier>();
            var logger = Substitute.For<IAppLogger<MediaGroupProcessor>>();

            var messages = new List<Message> { TelegramBuilder.TextMessage("album", userId: UserId, mediaGroupId: "g1") };
            buffer.TryTake(UserId, "g1", out Arg.Any<List<Message>>())
                .Returns(ci =>
                {
                    ci[2] = messages;
                    return true;
                });

            sessions.GetAsync(UserId, Arg.Any<CancellationToken>())
                .Returns(new UserSession { UserId = UserId, State = UserState.WaitingForAskQuery });

            handler.ProcessAsync(Arg.Any<List<Message>>(), Arg.Any<UserSession>(), Arg.Any<IUserRequestHistoryRepository>())
                .Returns<Task>(_ => throw new InvalidOperationException("handler failed"));

            var services = new ServiceCollection();
            services.AddSingleton(sessions);
            services.AddSingleton(history);
            services.AddSingleton(handler);
            services.AddSingleton(unexpectedError);
            var provider = services.BuildServiceProvider();
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

            var sut = new MediaGroupProcessor(queue, buffer, scopeFactory, logger);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await sut.StartAsync(cts.Token);

            await queue.EnqueueAsync(new MediaGroupWorkItem(UserId, "g1"));

            // Processor waits BufferDelay (2s) before handling each album.
            await WaitUntilAsync(
                () =>
                {
                    var calls = unexpectedError.ReceivedCalls()
                        .Count(c => c.GetMethodInfo().Name == nameof(IAskUnexpectedErrorNotifier.NotifyAsync));
                    return Task.FromResult(calls >= 1);
                },
                timeout: TimeSpan.FromSeconds(5));

            await unexpectedError.Received(1).NotifyAsync(UserId, UserId);

            cts.Cancel();
            await sut.StopAsync(CancellationToken.None);
        }

        private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (await condition())
                    return;
                await Task.Delay(50);
            }

            throw new TimeoutException("Condition was not met within timeout.");
        }

        private sealed class TestMediaGroupQueue : IMediaGroupQueue
        {
            private readonly Channel<MediaGroupWorkItem> _channel =
                Channel.CreateUnbounded<MediaGroupWorkItem>();

            public ChannelReader<MediaGroupWorkItem> Reader => _channel.Reader;

            public ValueTask EnqueueAsync(MediaGroupWorkItem item, CancellationToken cancellationToken = default) =>
                _channel.Writer.WriteAsync(item, cancellationToken);
        }
    }
}
