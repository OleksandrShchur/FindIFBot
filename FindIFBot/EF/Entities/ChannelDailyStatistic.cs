namespace FindIFBot.EF.Entities
{
    public class ChannelDailyStatistic
    {
        public int Id { get; init; }
        public DateOnly Date { get; set; }
        public int BotUserCount { get; set; }
        public int ChannelSubscriberCount { get; set; }
        public int PostsCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
