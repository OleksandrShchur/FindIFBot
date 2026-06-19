using FindIFBot.Domain;
using FindIFBot.EF.Entities;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers.Logs;

namespace FindIFBot.Services.Admin
{
    public class RequestHistoryStatusService : IRequestHistoryStatusService
    {
        private const string Component = "RequestHistory";

        private readonly IUserRequestHistoryRepository _history;
        private readonly IAppLogger<RequestHistoryStatusService> _logger;

        public RequestHistoryStatusService(
            IUserRequestHistoryRepository history,
            IAppLogger<RequestHistoryStatusService> logger)
        {
            _history = history;
            _logger = logger;
        }

        public async Task AddPendingAsync(long userId, int userMessageId)
        {
            var request = new UserRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = RequestStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                UserMessageId = userMessageId
            };

            await _history.Add(request);
        }

        public async Task<bool> TryTransitionAsync(long userId, int userMessageId, RequestStatus status, string? channelLink = null)
        {
            var transitioned = await _history.TryTransitionStatusAsync(
                userId, userMessageId, RequestStatus.Pending, status, channelLink);

            if (!transitioned)
            {
                await _logger.LogWarning(Component,
                    $"No pending request to transition (already moderated?) | UserId: {userId} | MessageId: {userMessageId} | Target: {status}");
                return false;
            }

            await _logger.LogInfo(Component,
                $"History updated to {status.ToString().ToUpperInvariant()} | UserId: {userId} | MessageId: {userMessageId}");
            return true;
        }

        public async Task SetChannelLinkAsync(long userId, int userMessageId, string channelLink)
        {
            await _history.SetChannelLinkAsync(userId, userMessageId, channelLink);
        }
    }
}
