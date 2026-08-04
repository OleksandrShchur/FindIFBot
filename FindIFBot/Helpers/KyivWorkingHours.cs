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

        public static DateOnly GetKyivDate(DateTimeOffset utcNow)
        {
            var local = TimeZoneInfo.ConvertTime(utcNow, KyivTimeZone);
            return DateOnly.FromDateTime(local.DateTime);
        }

        public static DateOnly GetKyivDate(TimeProvider timeProvider) =>
            GetKyivDate(timeProvider.GetUtcNow());

        public static (DateTime startUtc, DateTime endUtc) GetKyivDayUtcRange(DateOnly kyivDate)
        {
            var localStart = kyivDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            var localEnd = kyivDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, KyivTimeZone);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, KyivTimeZone);
            return (startUtc, endUtc);
        }

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
