using FindIFBot.Domain;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers.Logs;
using FindIFBot.Services.Admin;

namespace FindIFBot.UnitTests.Services.Admin
{
    public class AdminModerationServiceTests
    {
        private const long UserId = 321;
        private const int MessageId = 99;

        private readonly IUserSessionRepository _sessions = Substitute.For<IUserSessionRepository>();
        private readonly IUserModerationNotifier _userNotifier = Substitute.For<IUserModerationNotifier>();
        private readonly IAdminRequestNotifier _adminNotifier = Substitute.For<IAdminRequestNotifier>();
        private readonly IRequestPublisher _publisher = Substitute.For<IRequestPublisher>();
        private readonly IRequestHistoryStatusService _historyStatus = Substitute.For<IRequestHistoryStatusService>();
        private readonly IAppLogger<AdminModerationService> _logger = Substitute.For<IAppLogger<AdminModerationService>>();
        private readonly AdminModerationService _sut;

        public AdminModerationServiceTests()
        {
            _sut = new AdminModerationService(
                _sessions, _userNotifier, _adminNotifier, _publisher, _historyStatus, _logger);
        }

        [Fact]
        public async Task MarkAdvertisementAsync_WhenClaimed_TransitionsToAdvertisementAndNotifiesUser()
        {
            _historyStatus
                .TryTransitionAsync(UserId, MessageId, RequestStatus.Advertisement)
                .Returns(true);

            await _sut.MarkAdvertisementAsync(UserId, MessageId);

            await _historyStatus.Received(1)
                .TryTransitionAsync(UserId, MessageId, RequestStatus.Advertisement);
            await _userNotifier.Received(1).NotifyAdvertisementAsync(UserId, MessageId);
        }

        [Fact]
        public async Task MarkAdvertisementAsync_WhenAlreadyModerated_DoesNotNotifyUser()
        {
            _historyStatus
                .TryTransitionAsync(UserId, MessageId, RequestStatus.Advertisement)
                .Returns(false);

            await _sut.MarkAdvertisementAsync(UserId, MessageId);

            await _userNotifier.DidNotReceive()
                .NotifyAdvertisementAsync(Arg.Any<long>(), Arg.Any<int>());
        }

        [Fact]
        public async Task MarkNeedsAttentionAsync_WhenClaimed_TransitionsToNeedsAttentionAndNotifiesUser()
        {
            _historyStatus
                .TryTransitionAsync(UserId, MessageId, RequestStatus.NeedsAttention)
                .Returns(true);

            await _sut.MarkNeedsAttentionAsync(UserId, MessageId);

            await _historyStatus.Received(1)
                .TryTransitionAsync(UserId, MessageId, RequestStatus.NeedsAttention);
            await _userNotifier.Received(1).NotifyNeedsAttentionAsync(UserId, MessageId);
        }

        [Fact]
        public async Task MarkNeedsAttentionAsync_WhenAlreadyModerated_DoesNotNotifyUser()
        {
            _historyStatus
                .TryTransitionAsync(UserId, MessageId, RequestStatus.NeedsAttention)
                .Returns(false);

            await _sut.MarkNeedsAttentionAsync(UserId, MessageId);

            await _userNotifier.DidNotReceive()
                .NotifyNeedsAttentionAsync(Arg.Any<long>(), Arg.Any<int>());
        }
    }
}
