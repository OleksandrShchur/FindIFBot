using FindIFBot.Domain;

namespace FindIFBot.Services.Admin
{
    public interface IRequestHistoryStatusService
    {
        Task AddPendingAsync(long userId, int userMessageId);
        Task MarkAsync(long userId, int userMessageId, RequestStatus status, string? channelLink = null);
    }
}
