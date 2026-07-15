namespace FindIFBot.Services.Admin
{
    public interface IUserModerationNotifier
    {
        Task NotifySubmittedAsync(long chatId, int requestId);
        Task NotifyPublishedAsync(long userId, string channelLink, int requestId);
        Task NotifyRejectedAsync(long userId, int messageId);
        Task NotifyDuplicateAsync(long userId, int messageId);
        Task NotifyAdvertisementAsync(long userId, int messageId);
        Task NotifyNeedsAttentionAsync(long userId, int messageId);
        Task NotifyCancelledAsync(long userId, int messageId);
    }
}
