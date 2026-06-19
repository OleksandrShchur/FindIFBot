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
            // Claim the pending request atomically first. If another (double-delivered) callback
            // already claimed it, skip — this guarantees the post is published to the channel once.
            var claimed = await _historyStatus.TryTransitionAsync(userId, stored.MessageId, RequestStatus.Approved);
            if (!claimed)
            {
                await _logger.LogWarning(Component,
                    $"Publish skipped: request already moderated | UserId: {userId} | MessageId: {stored.MessageId}");
                return;
            }

            var channelLink = await _publisher.PublishAsync(stored);
            await _historyStatus.SetChannelLinkAsync(userId, stored.MessageId, channelLink);
            await _userNotifier.NotifyPublishedAsync(userId, channelLink);

            await _logger.LogInfo(Component,
                $"Request published | UserId: {userId} | MessageId: {stored.MessageId} | ChannelLink: {channelLink} | Photos: {stored.Photos.Count}");
        }

        public async Task RejectAsync(long userId, int messageId)
        {
            var claimed = await _historyStatus.TryTransitionAsync(userId, messageId, RequestStatus.Rejected);
            if (!claimed)
            {
                await _logger.LogWarning(Component,
                    $"Reject skipped: request already moderated | UserId: {userId} | MessageId: {messageId}");
                return;
            }

            await _userNotifier.NotifyRejectedAsync(userId, messageId);
            await _logger.LogInfo(Component, $"Request rejected | UserId: {userId} | MessageId: {messageId}");
        }

        public async Task MarkDuplicateAsync(long userId, int messageId)
        {
            var claimed = await _historyStatus.TryTransitionAsync(userId, messageId, RequestStatus.Duplicate);
            if (!claimed)
            {
                await _logger.LogWarning(Component,
                    $"Duplicate skipped: request already moderated | UserId: {userId} | MessageId: {messageId}");
                return;
            }

            await _userNotifier.NotifyDuplicateAsync(userId, messageId);
            await _logger.LogInfo(Component, $"Request marked as duplicate | UserId: {userId} | MessageId: {messageId}");
        }

        public async Task CancelAskAsync(long userId, int messageId)
        {
            await _userNotifier.NotifyCancelledAsync(userId, messageId);
            await _logger.LogInfo(Component, $"User cancelled ask request | UserId: {userId} | MessageId: {messageId}");

            var session = await _sessions.GetAsync(userId);
            session.State = UserState.Idle;
            await _sessions.SaveAsync(session);
        }
    }
}
