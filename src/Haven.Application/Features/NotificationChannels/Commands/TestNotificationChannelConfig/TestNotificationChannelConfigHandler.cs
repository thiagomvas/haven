using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.NotificationChannels.Commands.TestNotificationChannelConfig;

public sealed class TestNotificationChannelConfigHandler(
    INotificationChannelConfigRepository repository,
    IEnumerable<INotificationProvider> providers)
    : ICommandHandler<TestNotificationChannelConfigCommand, TestNotificationChannelConfigResult>
{
    private static readonly string TestPayload = JsonSerializer.Serialize(new
    {
        eventType = "TestEvent",
        occurredAt = DateTime.UtcNow,
        message = "This is a test notification from Haven.",
        data = new { }
    });

    public async ValueTask<Result<TestNotificationChannelConfigResult>> Handle(
        TestNotificationChannelConfigCommand command,
        CancellationToken cancellationToken)
    {
        var config = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (config is null)
            return Error.NotFoundFor(nameof(NotificationChannelConfig), command.Id);

        var provider = providers.FirstOrDefault(p => p.Channel == config.Channel);
        if (provider is null)
            return new TestNotificationChannelConfigResult(false, null, $"No provider registered for channel '{config.Channel}'.");

        var attempt = NotificationAttempt.CreateEnqueued(
            Guid.Empty, command.Id, config.Channel, "TestEvent", TestPayload);

        var result = await provider.SendAsync(attempt, config, cancellationToken);

        return new TestNotificationChannelConfigResult(result.Success, result.Response, result.ErrorMessage);
    }
}