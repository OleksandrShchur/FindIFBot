using FindIFBot.Configuration;
using Microsoft.Extensions.Options;

namespace FindIFBot.Helpers
{
    /// <summary>
    /// Evaluates working hours and local calendar days using configured timezone and hour bounds
    /// (defaults: Europe/Kyiv, 08:00–21:00).
    /// </summary>
    public sealed class KyivWorkingHours : IWorkingHours
    {
        private readonly WorkingHoursOptions _options;
        private readonly TimeZoneInfo _timeZone;

        public KyivWorkingHours(IOptions<WorkingHoursOptions> options)
            : this(options.Value)
        {
        }

        public KyivWorkingHours(WorkingHoursOptions options)
        {
            _options = options;
            _timeZone = ResolveTimeZone(options.TimeZoneId);
        }

        public int StartHour => _options.StartHour;
        public int EndHour => _options.EndHour;

        public bool IsWorkingHours(DateTimeOffset utcNow)
        {
            var local = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
            return local.Hour >= _options.StartHour && local.Hour < _options.EndHour;
        }

        public bool IsWorkingHours(TimeProvider timeProvider) =>
            IsWorkingHours(timeProvider.GetUtcNow());

        public DateOnly GetLocalDate(DateTimeOffset utcNow)
        {
            var local = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
            return DateOnly.FromDateTime(local.DateTime);
        }

        public DateOnly GetLocalDate(TimeProvider timeProvider) =>
            GetLocalDate(timeProvider.GetUtcNow());

        public (DateTime startUtc, DateTime endUtc) GetLocalDayUtcRange(DateOnly localDate)
        {
            var localStart = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            var localEnd = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, _timeZone);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, _timeZone);
            return (startUtc, endUtc);
        }

        /// <summary>Defaults used by tests and call sites that do not resolve DI.</summary>
        public static KyivWorkingHours CreateDefault() =>
            new(new WorkingHoursOptions());

        private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
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
