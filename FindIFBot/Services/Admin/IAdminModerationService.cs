using FindIFBot.Domain;
using FindIFBot.Persistence;

namespace FindIFBot.Services.Admin
{
    public interface IAdminModerationService
    {
        Task SubmitAskAsync(StoredMessage stored, UserInfo userInfo);
        Task PublishAsync(long userId, StoredMessage stored);
        Task RejectAsync(long userId, int messageId);
        Task MarkDuplicateAsync(long userId, int messageId);
        Task MarkAdvertisementAsync(long userId, int messageId);
        Task CancelAskAsync(long userId, int messageId);
    }
}
