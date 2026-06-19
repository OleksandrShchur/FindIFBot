using FindIFBot.Domain;
using FindIFBot.EF.Entities;

namespace FindIFBot.IntegrationTests.Repositories
{
    internal static class RequestBuilder
    {
        public static UserRequest Create(
            long userId = 100,
            int userMessageId = 1,
            RequestStatus status = RequestStatus.Pending,
            string? channelLink = null,
            DateTime? submittedAt = null,
            Guid? id = null) =>
            new()
            {
                Id = id ?? Guid.NewGuid(),
                UserId = userId,
                UserMessageId = userMessageId,
                Status = status,
                ChannelLink = channelLink,
                SubmittedAt = submittedAt ?? DateTime.UtcNow
            };
    }
}
