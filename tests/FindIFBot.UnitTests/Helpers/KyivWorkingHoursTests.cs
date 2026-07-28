using FindIFBot.Helpers;

namespace FindIFBot.UnitTests.Helpers
{
    public class KyivWorkingHoursTests
    {
        [Theory]
        [InlineData(8, 59, false)]
        [InlineData(9, 0, true)]
        [InlineData(12, 0, true)]
        [InlineData(21, 59, true)]
        [InlineData(22, 0, false)]
        [InlineData(23, 30, false)]
        [InlineData(0, 0, false)]
        public void IsWorkingHours_UsesKyivLocalTimeBoundaries(int hour, int minute, bool expected)
        {
            var utc = ToUtcFromKyiv(2026, 7, 28, hour, minute);

            KyivWorkingHours.IsWorkingHours(utc).Should().Be(expected);
        }

        private static DateTimeOffset ToUtcFromKyiv(int year, int month, int day, int hour, int minute)
        {
            var kyiv = ResolveKyivTimeZone();
            var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
            return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, kyiv), TimeSpan.Zero);
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
