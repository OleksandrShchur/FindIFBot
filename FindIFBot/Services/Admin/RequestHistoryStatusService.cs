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

        public async Task MarkAsync(long userId, int userMessageId, RequestStatus status, string? channelLink = null)
        {
            var requests = await _history.GetByUserId(userId);
            var request = requests.FirstOrDefault(r =>
                r.UserMessageId == userMessageId &&
                r.Status == RequestStatus.Pending);

            if (request == null)
            {
                return;
            }

            request.Status = status;
            request.ChannelLink = channelLink;
            await _history.Update(request);

            await _logger.LogInfo(Component,
                $"History updated to {status.ToString().ToUpperInvariant()} | UserId: {userId} | MessageId: {userMessageId}");
        }
    }
}
