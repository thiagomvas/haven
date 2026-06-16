using System.Text.Json;
using System.Text.Json.Nodes;

using Hangfire;
using Hangfire.States;

using Haven.Application.Common.Contracts.Notifications;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;
using Haven.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class HangfireNotificationEnqueuer(
    IBackgroundJobClient backgroundJobClient,
    INotificationAttemptRepository attemptRepository,
    IUnitOfWork unitOfWork,
    ILogger<HangfireNotificationEnqueuer> logger)
    : INotificationEnqueuer
{
    private const string NotificationsQueueName = "notifications";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Guid> EnqueueAsync(NotificationRule rule, DomainEvent domainEvent, CancellationToken ct = default)
    {
        var eventPayload = BuildPayload(domainEvent);

        var attempt = NotificationAttempt.CreateEnqueued(
            rule.Id,
            rule.ChannelConfigId,
            rule.ChannelConfig!.Channel,
            domainEvent.GetType().Name,
            eventPayload);

        var attemptId = await attemptRepository.AddAsync(attempt, ct);
        await unitOfWork.SaveChangesAsync(ct);

        backgroundJobClient.Create<NotificationDispatcherBackgroundJob>(
            x => x.ExecuteAsync(attemptId, CancellationToken.None),
            new EnqueuedState(NotificationsQueueName));

        logger.LogInformation(
            "Enqueued notification attempt {AttemptId} for event {EventType} via rule {RuleId}",
            attemptId, domainEvent.GetType().Name, rule.Id);

        return attemptId;
    }

    private static string BuildPayload(DomainEvent domainEvent)
    {
        var eventNode = JsonSerializer.SerializeToNode(domainEvent, domainEvent.GetType(), SerializerOptions);
        var obj = eventNode!.AsObject();
        obj.Remove("id");
        obj.Remove("occurredAt");
        obj.Remove("i18NKey");

        var envelope = new DomainEventEnvelope()
        {
            EventType = domainEvent.GetType().Name,
            OccuredAt = domainEvent.OccurredAt,
            Message = domainEvent.ToMessage(),
            Data = eventNode
        };

        return JsonSerializer.Serialize(envelope, SerializerOptions);
    }
}