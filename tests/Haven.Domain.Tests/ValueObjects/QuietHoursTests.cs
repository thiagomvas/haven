using Haven.Domain.Exceptions;
using Haven.Domain.ValueObjects;

using Shouldly;

namespace Haven.Domain.Tests.ValueObjects;

[Category("Unit")]
public sealed class QuietHoursTests
{
    private const string Berlin = "Europe/Berlin";

    private static readonly DayOfWeek[] EveryDay = Enum.GetValues<DayOfWeek>();

    private static DateTime Utc(int year, int month, int day, int hour, int minute = 0) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private static QuietHours Nightly(string zone = Berlin, params DayOfWeek[] days) =>
        QuietHours.From(days.Length == 0 ? EveryDay : days, new TimeOnly(22, 0), new TimeOnly(6, 0), zone);

    [Test]
    public void From_WithoutDays_ShouldThrow() =>
        Should.Throw<ValidationException>(() => QuietHours.From([], new TimeOnly(22, 0), new TimeOnly(6, 0), Berlin));

    [TestCase("")]
    [TestCase("Mars/Olympus_Mons")]
    public void From_WithUnknownZone_ShouldThrow(string zone) =>
        Should.Throw<ValidationException>(() => Nightly(zone));

    [Test]
    public void From_ShouldNormaliseDaysAndCompareStructurally()
    {
        var first = Nightly(Berlin, DayOfWeek.Friday, DayOfWeek.Monday, DayOfWeek.Friday);
        var second = Nightly(Berlin, DayOfWeek.Monday, DayOfWeek.Friday);

        first.Days.ShouldBe([DayOfWeek.Monday, DayOfWeek.Friday]);
        first.ShouldBe(second);
    }

    [TestCase(20, 59, false)]
    [TestCase(21, 0, true)]
    [TestCase(23, 30, true)]
    [TestCase(4, 59, true)]
    [TestCase(5, 0, false)]
    public void IsQuietAt_WindowCrossingMidnight_ShouldCoverBothDates(int utcHour, int utcMinute, bool expected)
    {
        var quietHours = Nightly(Berlin, DayOfWeek.Friday);
        var day = utcHour >= 12 ? 16 : 17;

        quietHours.IsQuietAt(Utc(2026, 1, day, utcHour, utcMinute)).ShouldBe(expected);
    }

    [Test]
    public void IsQuietAt_MorningAfterInactiveDay_ShouldBeLoud()
    {
        var quietHours = Nightly(Berlin, DayOfWeek.Friday);

        quietHours.IsQuietAt(Utc(2026, 1, 16, 3)).ShouldBeFalse();
    }

    [Test]
    public void EndsAt_ShouldReturnLocalEndInUtc()
    {
        var quietHours = Nightly(Berlin, DayOfWeek.Friday);

        quietHours.EndsAt(Utc(2026, 1, 16, 23)).ShouldBe(Utc(2026, 1, 17, 5));
        quietHours.EndsAt(Utc(2026, 1, 17, 12)).ShouldBeNull();
    }

    [Test]
    public void EndsAt_SpringForwardNight_ShouldUseSummerOffsetForTheEnd()
    {
        var quietHours = Nightly();

        quietHours.EndsAt(Utc(2026, 3, 28, 23)).ShouldBe(Utc(2026, 3, 29, 4));
        quietHours.IsQuietAt(Utc(2026, 3, 29, 3, 59)).ShouldBeTrue();
        quietHours.IsQuietAt(Utc(2026, 3, 29, 4)).ShouldBeFalse();
    }

    [Test]
    public void EndsAt_FallBackNight_ShouldUseWinterOffsetForTheEnd()
    {
        var quietHours = Nightly();

        quietHours.EndsAt(Utc(2026, 10, 24, 21)).ShouldBe(Utc(2026, 10, 25, 5));
        quietHours.IsQuietAt(Utc(2026, 10, 25, 4, 59)).ShouldBeTrue();
    }

    [Test]
    public void EndsAt_WindowStartingInsideSpringForwardGap_ShouldStartAtFirstValidMinute()
    {
        var quietHours = QuietHours.From([DayOfWeek.Sunday], new TimeOnly(2, 30), new TimeOnly(5, 0), Berlin);

        quietHours.IsQuietAt(Utc(2026, 3, 29, 0, 59)).ShouldBeFalse();
        quietHours.IsQuietAt(Utc(2026, 3, 29, 1, 0)).ShouldBeTrue();
        quietHours.EndsAt(Utc(2026, 3, 29, 1, 0)).ShouldBe(Utc(2026, 3, 29, 3));
    }

    [Test]
    public void EndsAt_WindowEndingInsideFallBackHour_ShouldUseTheLaterInstant()
    {
        var quietHours = QuietHours.From([DayOfWeek.Sunday], new TimeOnly(1, 0), new TimeOnly(2, 30), Berlin);

        quietHours.EndsAt(Utc(2026, 10, 25, 0, 30)).ShouldBe(Utc(2026, 10, 25, 1, 30));
    }

    [Test]
    public void EndsAt_StartEqualToEnd_ShouldCoverTheWholeDay()
    {
        var quietHours = QuietHours.From([DayOfWeek.Saturday], TimeOnly.MinValue, TimeOnly.MinValue, Berlin);

        quietHours.IsQuietAt(Utc(2026, 1, 16, 22, 59)).ShouldBeFalse();
        quietHours.EndsAt(Utc(2026, 1, 16, 23)).ShouldBe(Utc(2026, 1, 17, 23));
        quietHours.IsQuietAt(Utc(2026, 1, 17, 23)).ShouldBeFalse();
    }

    [Test]
    public void EndsAt_BackToBackWindows_ShouldReturnTheEndOfTheLastOne()
    {
        var quietHours = QuietHours.From(
            [DayOfWeek.Saturday, DayOfWeek.Sunday], TimeOnly.MinValue, TimeOnly.MinValue, "UTC");

        quietHours.EndsAt(Utc(2026, 1, 17, 10)).ShouldBe(Utc(2026, 1, 19, 0));
    }

    [Test]
    public void IsQuietAt_ShouldTreatUnspecifiedKindAsUtc()
    {
        var quietHours = Nightly("UTC");
        var unspecified = new DateTime(2026, 1, 16, 23, 0, 0, DateTimeKind.Unspecified);

        quietHours.IsQuietAt(unspecified).ShouldBeTrue();
    }
}