using Haven.Domain.Entities;
using Haven.Domain.Enums;

using Shouldly;

namespace Haven.Domain.Tests.Entities;

[Category("Unit")]
public sealed class NotificationAttemptHoldTests
{
    private static readonly DateTime ReleaseAt = new(2026, 11, 23, 5, 0, 0, DateTimeKind.Utc);

    private static NotificationAttempt PendingAttempt() =>
        NotificationAttempt.CreateEnqueued(Guid.NewGuid(), Guid.NewGuid(), NotificationChannel.Webhook, "ServiceCrashedEvent", "{}");

    [Test]
    public void HoldUntil_ShouldMarkHeldAndKeepReleaseTime()
    {
        var attempt = PendingAttempt();

        attempt.HoldUntil(ReleaseAt);

        attempt.Status.ShouldBe(NotificationDeliveryStatus.Held);
        attempt.HeldUntil.ShouldBe(ReleaseAt);
    }

    [Test]
    public void HoldUntil_WhenAlreadyDelivered_ShouldThrow()
    {
        var attempt = PendingAttempt();
        attempt.MarkDelivered("{}", "200 OK");

        Should.Throw<InvalidOperationException>(() => attempt.HoldUntil(ReleaseAt));
    }

    [Test]
    public void Release_ShouldReturnHeldAttemptToPendingAndKeepHeldUntilForHistory()
    {
        var attempt = PendingAttempt();
        attempt.HoldUntil(ReleaseAt);

        attempt.Release();

        attempt.Status.ShouldBe(NotificationDeliveryStatus.Pending);
        attempt.HeldUntil.ShouldBe(ReleaseAt);
    }

    [Test]
    public void Release_WhenNotHeld_ShouldLeaveStatusAlone()
    {
        var attempt = PendingAttempt();
        attempt.MarkFailed("{}", null, "boom");

        attempt.Release();

        attempt.Status.ShouldBe(NotificationDeliveryStatus.Failed);
    }
}