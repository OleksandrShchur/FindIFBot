namespace FindIFBot.EF.Entities
{
    /// <summary>
    /// Singleton row tracking the next admin pending-queue reminder due time.
    /// Always stored with <see cref="Id"/> = 1.
    /// </summary>
    public class AdminQueueReminderState
    {
        public const int SingletonId = 1;

        public int Id { get; init; } = SingletonId;
        public DateTime? DueAtUtc { get; set; }
        public DateTime? LastSentAtUtc { get; set; }
    }
}
