namespace FindIFBot.Domain
{
    public class ReplyHistoryMessage
    {
        public int MessageId { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
