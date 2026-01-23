using FindIFBot.Domain;

namespace FindIFBot.EF.Entities
{
    public class UserSession
    {
        public long UserId { get; init; }
        public UserState State { get; set; } = UserState.Idle;
        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
    }
}
