using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Shouldly;

namespace Haven.Domain.Tests.Entities;

[Category("Unit")]
public sealed class NotificationChannelConfigQuietHoursTests
{
    private static NotificationChannelConfig Channel() =>
        NotificationChannelConfig.Create("ops", NotificationChannel.Discord, "{}", enabled: true);

    private static QuietHours NightlyInBerlin() =>
        QuietHours.From(Enum.GetValues<DayOfWeek>(), new TimeOnly(22, 0), new TimeOnly(6, 0), "Europe/Berlin");

    [Test]
    public void QuietUntil_WithoutQuietHours_ShouldBeNull() =>
        Channel().QuietUntil(new DateTime(2026, 11, 22, 23, 0, 0, DateTimeKind.Utc)).ShouldBeNull();

    [Test]
    public void QuietUntil_InsideWindow_ShouldReturnWindowEndInUtc()
    {
        var channel = Channel();
        channel.SetQuietHours(NightlyInBerlin());

        var until = channel.QuietUntil(new DateTime(2026, 11, 22, 23, 0, 0, DateTimeKind.Utc));

        until.ShouldBe(new DateTime(2026, 11, 23, 5, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public void QuietUntil_OutsideWindow_ShouldBeNull()
    {
        var channel = Channel();
        channel.SetQuietHours(NightlyInBerlin());

        channel.QuietUntil(new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc)).ShouldBeNull();
    }

    [Test]
    public void SetQuietHours_WithNull_ShouldClearTheWindow()
    {
        var channel = Channel();
        channel.SetQuietHours(NightlyInBerlin());

        channel.SetQuietHours(null);

        channel.QuietHours.ShouldBeNull();
    }
}