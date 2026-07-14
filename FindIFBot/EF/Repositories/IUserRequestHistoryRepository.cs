using FindIFBot.Domain;
using FindIFBot.EF.Entities;

namespace FindIFBot.EF.Repositories
{
    public interface IUserRequestHistoryRepository
    {
        Task Add(UserRequest request);
        Task Update(UserRequest request);
        Task<List<UserRequest>> GetByUserId(long userId);
        Task<UserRequest?> GetById(Guid id);
        Task<bool> HasHistory(long userId);

        /// <summary>
        /// Returns the newest pending requests across all users, capped at <paramref name="limit"/>.
        /// </summary>
        Task<List<UserRequest>> GetPendingAsync(int limit);

        /// <summary>
        /// Atomically transitions a request from <paramref name="expectedStatus"/> to
        /// <paramref name="newStatus"/> in a single SQL UPDATE. Returns true only if exactly this
        /// caller performed the transition, providing idempotency for double-delivered callbacks.
        /// </summary>
        Task<bool> TryTransitionStatusAsync(
            long userId,
            int userMessageId,
            RequestStatus expectedStatus,
            RequestStatus newStatus,
            string? channelLink,
            CancellationToken cancellationToken = default);

        /// <summary>Sets the channel link on an already-approved request (post-publish).</summary>
        Task SetChannelLinkAsync(
            long userId,
            int userMessageId,
            string channelLink,
            CancellationToken cancellationToken = default);
    }
}
