using FindIFBot.Configuration;
using FindIFBot.Helpers;

namespace FindIFBot.UnitTests.Helpers
{
    public class KyivWorkingHoursTests
    {
        private readonly KyivWorkingHours _sut = KyivWorkingHours.CreateDefault();

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

            _sut.IsWorkingHours(utc).Should().Be(expected);
        }

        [Fact]
        public void GetLocalDate_ConvertsUtcAcrossMidnightBoundary()
        {
            // 2026-07-28 23:30 Kyiv is still 28th; 00:30 next day is 29th.
            var lateEvening = ToUtcFromKyiv(2026, 7, 28, 23, 30);
            var afterMidnight = ToUtcFromKyiv(2026, 7, 29, 0, 30);

            _sut.GetLocalDate(lateEvening).Should().Be(new DateOnly(2026, 7, 28));
            _sut.GetLocalDate(afterMidnight).Should().Be(new DateOnly(2026, 7, 29));
        }

        [Fact]
        public void GetLocalDayUtcRange_IsHalfOpenAndCoversFullLocalDay()
        {
            var date = new DateOnly(2026, 7, 28);
            var (startUtc, endUtc) = _sut.GetLocalDayUtcRange(date);

            _sut.GetLocalDate(new DateTimeOffset(startUtc, TimeSpan.Zero))
                .Should().Be(date);
            _sut.GetLocalDate(new DateTimeOffset(endUtc.AddTicks(-1), TimeSpan.Zero))
                .Should().Be(date);
            _sut.GetLocalDate(new DateTimeOffset(endUtc, TimeSpan.Zero))
                .Should().Be(date.AddDays(1));
            (endUtc - startUtc).Should().Be(TimeSpan.FromHours(24));
        }

        [Fact]
        public void IsWorkingHours_UsesConfiguredHourBounds()
        {
            var custom = new KyivWorkingHours(new WorkingHoursOptions
            {
                StartHour = 10,
                EndHour = 18,
                TimeZoneId = "Europe/Kyiv"
            });

            custom.IsWorkingHours(ToUtcFromKyiv(2026, 7, 28, 9, 30)).Should().BeFalse();
            custom.IsWorkingHours(ToUtcFromKyiv(2026, 7, 28, 10, 0)).Should().BeTrue();
            custom.IsWorkingHours(ToUtcFromKyiv(2026, 7, 28, 18, 0)).Should().BeFalse();
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
