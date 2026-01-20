using FindIFBot.Persistence;

namespace FindIFBot.Domain
{
    public enum RequestStatus
    {
        Pending,
        Approved,
        Rejected,
        Duplicate
    }

    public class UserRequest
    {
        public Guid Id { get; init; }
        public long UserId { get; init; }
        public StoredMessage StoredMessage { get; init; }
        public RequestStatus Status { get; set; }
        public string? ChannelLink { get; set; }
        public DateTime SubmittedAt { get; init; }
        public int UserMessageId { get; init; } // message id in user's chat
    }
}