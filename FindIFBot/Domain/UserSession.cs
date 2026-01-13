namespace FindIFBot.Domain
{
    public class UserSession
    {
        public long UserId { get; init; }
        public UserState State { get; set; } = UserState.Idle;
    }
}
