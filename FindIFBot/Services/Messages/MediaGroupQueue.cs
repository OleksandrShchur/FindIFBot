using System.Threading.Channels;

namespace FindIFBot.Services.Messages
{
    /// <summary>
    /// Process-wide queue of media groups awaiting processing. Backed by an unbounded channel;
    /// a single hosted <see cref="MediaGroupProcessor"/> drains it. This replaces the previous
    /// untracked <c>Task.Run</c> fire-and-forget so failures are observed and shutdown is graceful.
    /// </summary>
    public class MediaGroupQueue : IMediaGroupQueue
    {
        private readonly Channel<MediaGroupWorkItem> _channel =
            Channel.CreateUnbounded<MediaGroupWorkItem>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        public ValueTask EnqueueAsync(MediaGroupWorkItem item, CancellationToken cancellationToken = default) =>
            _channel.Writer.WriteAsync(item, cancellationToken);

        public ChannelReader<MediaGroupWorkItem> Reader => _channel.Reader;
    }
}
