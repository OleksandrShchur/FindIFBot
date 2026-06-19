using FindIFBot.Domain;
using FindIFBot.EF.Repositories;

namespace FindIFBot.IntegrationTests.Repositories
{
    public class UserRequestHistoryRepositoryTests
    {
        [Fact]
        public async Task Given_Request_When_Add_Then_Persists()
        {
            using var db = new SqliteTestDatabase();
            var request = RequestBuilder.Create(userId: 100, userMessageId: 5);

            await new UserRequestHistoryRepository(db.CreateContext()).Add(request);

            var stored = await new UserRequestHistoryRepository(db.CreateContext()).GetById(request.Id);
            stored.Should().NotBeNull();
            stored!.UserId.Should().Be(100);
            stored.UserMessageId.Should().Be(5);
        }

        [Fact]
        public async Task Given_StoredRequest_When_Update_Then_PersistsChanges()
        {
            using var db = new SqliteTestDatabase();
            var request = RequestBuilder.Create(status: RequestStatus.Pending);
            await new UserRequestHistoryRepository(db.CreateContext()).Add(request);

            var toUpdate = await new UserRequestHistoryRepository(db.CreateContext()).GetById(request.Id);
            toUpdate!.Status = RequestStatus.Approved;
            toUpdate.ChannelLink = "https://t.me/c/1";
            await new UserRequestHistoryRepository(db.CreateContext()).Update(toUpdate);

            var stored = await new UserRequestHistoryRepository(db.CreateContext()).GetById(request.Id);
            stored!.Status.Should().Be(RequestStatus.Approved);
            stored.ChannelLink.Should().Be("https://t.me/c/1");
        }

        [Fact]
        public async Task Given_MultipleRequests_When_GetByUserId_Then_OrderedBySubmittedAtDescending()
        {
            using var db = new SqliteTestDatabase();
            var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var oldest = RequestBuilder.Create(userId: 100, userMessageId: 1, submittedAt: baseTime);
            var newest = RequestBuilder.Create(userId: 100, userMessageId: 2, submittedAt: baseTime.AddDays(2));
            var middle = RequestBuilder.Create(userId: 100, userMessageId: 3, submittedAt: baseTime.AddDays(1));
            var other = RequestBuilder.Create(userId: 200, userMessageId: 4, submittedAt: baseTime.AddDays(5));

            var repo = new UserRequestHistoryRepository(db.CreateContext());
            await repo.Add(oldest);
            await new UserRequestHistoryRepository(db.CreateContext()).Add(newest);
            await new UserRequestHistoryRepository(db.CreateContext()).Add(middle);
            await new UserRequestHistoryRepository(db.CreateContext()).Add(other);

            var result = await new UserRequestHistoryRepository(db.CreateContext()).GetByUserId(100);

            result.Select(r => r.UserMessageId).Should().ContainInOrder(2, 3, 1);
            result.Should().OnlyContain(r => r.UserId == 100);
        }

        [Fact]
        public async Task Given_Request_When_GetById_Then_ReturnsCorrectEntity()
        {
            using var db = new SqliteTestDatabase();
            var a = RequestBuilder.Create(userMessageId: 1);
            var b = RequestBuilder.Create(userMessageId: 2);
            await new UserRequestHistoryRepository(db.CreateContext()).Add(a);
            await new UserRequestHistoryRepository(db.CreateContext()).Add(b);

            var result = await new UserRequestHistoryRepository(db.CreateContext()).GetById(b.Id);

            result!.Id.Should().Be(b.Id);
            result.UserMessageId.Should().Be(2);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Given_UserWithOrWithoutRequests_When_HasHistory_Then_ReturnsExpected(bool seed)
        {
            using var db = new SqliteTestDatabase();
            if (seed)
                await new UserRequestHistoryRepository(db.CreateContext()).Add(RequestBuilder.Create(userId: 100));

            var result = await new UserRequestHistoryRepository(db.CreateContext()).HasHistory(100);

            result.Should().Be(seed);
        }

        [Fact]
        public async Task Given_MatchingStatus_When_TryTransition_Then_SucceedsAndUpdates()
        {
            using var db = new SqliteTestDatabase();
            var request = RequestBuilder.Create(userId: 100, userMessageId: 5, status: RequestStatus.Pending);
            await new UserRequestHistoryRepository(db.CreateContext()).Add(request);

            var transitioned = await new UserRequestHistoryRepository(db.CreateContext())
                .TryTransitionStatusAsync(100, 5, RequestStatus.Pending, RequestStatus.Approved, "https://t.me/c/9");

            transitioned.Should().BeTrue();
            var stored = await new UserRequestHistoryRepository(db.CreateContext()).GetById(request.Id);
            stored!.Status.Should().Be(RequestStatus.Approved);
            stored.ChannelLink.Should().Be("https://t.me/c/9");
        }

        [Fact]
        public async Task Given_NonMatchingStatus_When_TryTransition_Then_FailsAndLeavesUnchanged()
        {
            using var db = new SqliteTestDatabase();
            var request = RequestBuilder.Create(userId: 100, userMessageId: 5, status: RequestStatus.Approved);
            await new UserRequestHistoryRepository(db.CreateContext()).Add(request);

            var transitioned = await new UserRequestHistoryRepository(db.CreateContext())
                .TryTransitionStatusAsync(100, 5, RequestStatus.Pending, RequestStatus.Rejected, null);

            transitioned.Should().BeFalse();
            var stored = await new UserRequestHistoryRepository(db.CreateContext()).GetById(request.Id);
            stored!.Status.Should().Be(RequestStatus.Approved);
        }

        [Fact]
        public async Task Given_ApprovedRequest_When_SetChannelLink_Then_OnlyApprovedUpdated()
        {
            using var db = new SqliteTestDatabase();
            var approved = RequestBuilder.Create(userId: 100, userMessageId: 1, status: RequestStatus.Approved);
            var pending = RequestBuilder.Create(userId: 100, userMessageId: 2, status: RequestStatus.Pending);
            await new UserRequestHistoryRepository(db.CreateContext()).Add(approved);
            await new UserRequestHistoryRepository(db.CreateContext()).Add(pending);

            await new UserRequestHistoryRepository(db.CreateContext())
                .SetChannelLinkAsync(100, 1, "https://t.me/c/approved");
            await new UserRequestHistoryRepository(db.CreateContext())
                .SetChannelLinkAsync(100, 2, "https://t.me/c/pending");

            var repo = new UserRequestHistoryRepository(db.CreateContext());
            (await repo.GetById(approved.Id))!.ChannelLink.Should().Be("https://t.me/c/approved");
            (await new UserRequestHistoryRepository(db.CreateContext()).GetById(pending.Id))!.ChannelLink.Should().BeNull();
        }
    }
}
