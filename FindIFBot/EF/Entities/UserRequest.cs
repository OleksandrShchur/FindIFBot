using FindIFBot.Domain;

namespace FindIFBot.EF.Entities
{
    public class UserRequest
    {
        public Guid Id { get; init; }
        public long UserId { get; init; }
        public RequestStatus Status { get; set; }
        public string? ChannelLink { get; set; }
        public DateTime SubmittedAt { get; init; }
        public int UserMessageId { get; init; } // message id in user's chat
    }
}