namespace FindIFBot.Helpers
{
    public static class KyivWorkingHours
    {
        private const int StartHour = 9;
        private const int EndHour = 22;

        private static readonly TimeZoneInfo KyivTimeZone = ResolveKyivTimeZone();

        public static bool IsWorkingHours(DateTimeOffset utcNow)
        {
            var local = TimeZoneInfo.ConvertTime(utcNow, KyivTimeZone);
            return local.Hour >= StartHour && local.Hour < EndHour;
        }

        public static bool IsWorkingHours(TimeProvider timeProvider) =>
            IsWorkingHours(timeProvider.GetUtcNow());

        private static TimeZoneInfo ResolveKyivTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
            }
        }
    }
}
