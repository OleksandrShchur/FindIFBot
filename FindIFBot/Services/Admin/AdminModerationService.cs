using FindIFBot.Domain;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers.Logs;
using FindIFBot.Persistence;

namespace FindIFBot.Services.Admin
{
    public class AdminModerationService : IAdminModerationService
    {
        private const string Component = "AdminModeration";

        private readonly IUserSessionRepository _sessions;
        private readonly IUserModerationNotifier _userNotifier;
        private readonly IAdminRequestNotifier _adminNotifier;
        private readonly IRequestPublisher _publisher;
        private readonly IRequestHistoryStatusService _historyStatus;
        private readonly IAppLogger<AdminModerationService> _logger;

        public AdminModerationService(
            IUserSessionRepository sessions,
            IUserModerationNotifier userNotifier,
            IAdminRequestNotifier adminNotifier,
            IRequestPublisher publisher,
            IRequestHistoryStatusService historyStatus,
            IAppLogger<AdminModerationService> logger)
        {
            _sessions = sessions;
            _userNotifier = userNotifier;
            _adminNotifier = adminNotifier;
            _publisher = publisher;
            _historyStatus = historyStatus;
            _logger = logger;
        }

        public async Task SubmitAskAsync(StoredMessage stored, UserInfo userInfo)
        {
            await _logger.LogInfo(Component,
                $"Submitting ask request to moderation | UserId: {stored.UserId} | MessageId: {stored.MessageId} | Photos: {stored.Photos.Count}");

            await _userNotifier.NotifySubmittedAsync(stored.ChatId);
            await _historyStatus.AddPendingAsync(stored.UserId, stored.MessageId);
            await _adminNotifier.SendToAdminAsync(stored, userInfo);
        }

        public async Task PublishAsync(long userId, StoredMessage stored)
        {
            var channelLink = await _publisher.PublishAsync(stored);
            await _userNotifier.NotifyPublishedAsync(userId, channelLink);

            await _logger.LogInfo(Component,
                $"Request published | UserId: {userId} | MessageId: {stored.MessageId} | ChannelLink: {channelLink} | Photos: {stored.Photos.Count}");

            await _historyStatus.MarkAsync(userId, stored.MessageId, RequestStatus.Approved, channelLink);
        }

        public async Task RejectAsync(long userId, int messageId)
        {
            await _userNotifier.NotifyRejectedAsync(userId, messageId);
            await _logger.LogInfo(Component, $"Request rejected | UserId: {userId} | MessageId: {messageId}");
            await _historyStatus.MarkAsync(userId, messageId, RequestStatus.Rejected);
        }

        public async Task MarkDuplicateAsync(long userId, int messageId)
        {
            await _userNotifier.NotifyDuplicateAsync(userId, messageId);
            await _logger.LogInfo(Component, $"Request marked as duplicate | UserId: {userId} | MessageId: {messageId}");
            await _historyStatus.MarkAsync(userId, messageId, RequestStatus.Duplicate);
        }

        public async Task CancelAskAsync(long userId, int messageId)
        {
            await _userNotifier.NotifyCancelledAsync(userId, messageId);
            await _logger.LogInfo(Component, $"User cancelled ask request | UserId: {userId} | MessageId: {messageId}");

            var session = _sessions.Get(userId);
            session.State = UserState.Idle;
            _sessions.Save(session);
        }
    }
}
