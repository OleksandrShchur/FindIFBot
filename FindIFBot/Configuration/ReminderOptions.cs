namespace FindIFBot.Configuration
{
    /// <summary>
    /// Admin pending-queue reminder settings. Bound from the "Reminder" configuration section.
    /// </summary>
    public sealed class ReminderOptions
    {
        public const string SectionName = "Reminder";

        /// <summary>Minutes to wait before (re)sending a reminder while the queue is non-empty.</summary>
        public int DelayMinutes { get; init; } = 30;
    }
}
