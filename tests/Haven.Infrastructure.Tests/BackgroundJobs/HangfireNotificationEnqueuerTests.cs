using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Events;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.BackgroundJobs;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.BackgroundJobs;

[Category("Unit")]
public sealed class HangfireNotificationEnqueuerTests
{
    private sealed record Harness(
        HangfireNotificationEnqueuer Enqueuer,
        IBackgroundJobClient JobClient,
        List<NotificationAttempt> Added);

    private static Harness CreateHarness()
    {
        var jobClient = Substitute.For<IBackgroundJobClient>();
        var repository = Substitute.For<INotificationAttemptRepository>();
        var added = new List<NotificationAttempt>();
        repository.AddAsync(Arg.Do<NotificationAttempt>(added.Add), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        var enqueuer = new HangfireNotificationEnqueuer(
            jobClient,
            repository,
            Substitute.For<IUnitOfWork>(),
            Substitute.For<ILogger<HangfireNotificationEnqueuer>>());

        return new Harness(enqueuer, jobClient, added);
    }

    private static NotificationRule RuleFor(NotificationChannelConfig channel) =>
        new() { ChannelConfigId = channel.Id, ChannelConfig = channel, EventType = nameof(ProjectCreatedEvent), Enabled = true };

    private static NotificationChannelConfig Channel() =>
        NotificationChannelConfig.Create("ops", NotificationChannel.Discord, "{}", enabled: true);

    private static ProjectCreatedEvent SomeEvent() => new(Guid.NewGuid(), "demo");

    [Test]
    public async Task EnqueueAsync_WithoutQuietHours_ShouldEnqueueOnNotificationsQueue()
    {
        var harness = CreateHarness();

        await harness.Enqueuer.EnqueueAsync(RuleFor(Channel()), SomeEvent());

        harness.Added.Single().Status.ShouldBe(NotificationDeliveryStatus.Pending);
        harness.Added.Single().HeldUntil.ShouldBeNull();
        harness.JobClient.Received(1).Create(
            Arg.Any<Job>(),
            Arg.Is<IState>(state => state is EnqueuedState && ((EnqueuedState)state).Queue == "notifications"));
    }

    [Test]
    public async Task EnqueueAsync_DuringQuietHours_ShouldHoldAttemptAndScheduleDispatchAtWindowEnd()
    {
        var harness = CreateHarness();
        var channel = Channel();
        channel.SetQuietHours(QuietHours.From(Enum.GetValues<DayOfWeek>(), new TimeOnly(0, 0), new TimeOnly(0, 0), "UTC"));
        var expectedRelease = channel.QuietUntil(DateTime.UtcNow)!.Value;

        await harness.Enqueuer.EnqueueAsync(RuleFor(channel), SomeEvent());

        var attempt = harness.Added.Single();
        attempt.Status.ShouldBe(NotificationDeliveryStatus.Held);
        attempt.HeldUntil.ShouldNotBeNull();
        attempt.HeldUntil.Value.ShouldBeGreaterThanOrEqualTo(expectedRelease);
        harness.JobClient.Received(1).Create(
            Arg.Is<Job>(job => job.Type == typeof(NotificationDispatcherBackgroundJob)),
            Arg.Is<IState>(state => state is ScheduledState && ((ScheduledState)state).EnqueueAt == attempt.HeldUntil.Value));
    }
}