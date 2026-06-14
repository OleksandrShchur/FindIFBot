using System.Threading.Channels;

namespace FindIFBot.Services.Messages
{
    public interface IMediaGroupQueue
    {
        ValueTask EnqueueAsync(MediaGroupWorkItem item, CancellationToken cancellationToken = default);
        ChannelReader<MediaGroupWorkItem> Reader { get; }
    }
}
