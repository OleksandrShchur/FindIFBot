namespace FindIFBot.Helpers
{
    public interface IWorkingHours
    {
        bool IsWorkingHours(DateTimeOffset utcNow);
        bool IsWorkingHours(TimeProvider timeProvider);
        DateOnly GetLocalDate(DateTimeOffset utcNow);
        DateOnly GetLocalDate(TimeProvider timeProvider);
        (DateTime startUtc, DateTime endUtc) GetLocalDayUtcRange(DateOnly localDate);
        int StartHour { get; }
        int EndHour { get; }
    }
}
