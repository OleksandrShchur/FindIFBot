namespace FindIFBot.Services.Admin
{
    public interface IUserModerationNotifier
    {
        Task NotifySubmittedAsync(long chatId);
        Task NotifyPublishedAsync(long userId, string channelLink);
        Task NotifyRejectedAsync(long userId, int messageId);
        Task NotifyDuplicateAsync(long userId, int messageId);
        Task NotifyAdvertisementAsync(long userId, int messageId);
        Task NotifyNeedsAttentionAsync(long userId, int messageId);
        Task NotifyCancelledAsync(long userId, int messageId);
    }
}
