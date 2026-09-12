namespace FindIFBot.Configuration
{
    /// <summary>
    /// Local working-hours window used for user submit messaging and admin queue reminders.
    /// Bound from the "WorkingHours" configuration section.
    /// </summary>
    public sealed class WorkingHoursOptions
    {
        public const string SectionName = "WorkingHours";

        public int StartHour { get; init; } = 8;
        public int EndHour { get; init; } = 21;
        public string TimeZoneId { get; init; } = "Europe/Kyiv";
    }
}
