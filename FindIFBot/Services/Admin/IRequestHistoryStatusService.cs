using FindIFBot.Domain;

namespace FindIFBot.Services.Admin
{
    public interface IRequestHistoryStatusService
    {
        Task AddPendingAsync(long userId, int userMessageId, int adminInfoMessageId);

        /// <summary>
        /// Atomically transitions the user's pending request to <paramref name="status"/>.
        /// Returns false when no pending request matched (e.g. it was already moderated),
        /// allowing callers to make moderation actions idempotent.
        /// </summary>
        Task<bool> TryTransitionAsync(long userId, int userMessageId, RequestStatus status, string? channelLink = null);

        /// <summary>Updates the channel link on an approved request after it has been published.</summary>
        Task SetChannelLinkAsync(long userId, int userMessageId, string channelLink);
    }
}
