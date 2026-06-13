using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.NotificationChannels.Commands.TestNotificationChannelConfig;
using Haven.Domain.Entities;

namespace Haven.Application.Features.NotificationChannels.Commands.TestNotificationChannelConfigInline;

public sealed class TestNotificationChannelConfigInlineHandler(IEnumerable<INotificationProvider> providers)
    : ICommandHandler<TestNotificationChannelConfigInlineCommand, TestNotificationChannelConfigResult>
{
    private static readonly string TestPayload = JsonSerializer.Serialize(new
    {
        eventType = "TestEvent",
        occurredAt = DateTime.UtcNow,
        message = "This is a test notification from Haven.",
        data = new { }
    });

    public async ValueTask<Result<TestNotificationChannelConfigResult>> Handle(
        TestNotificationChannelConfigInlineCommand command,
        CancellationToken cancellationToken)
    {
        var provider = providers.FirstOrDefault(p => p.Channel == command.Channel);
        if (provider is null)
            return new TestNotificationChannelConfigResult(false, null, $"No provider registered for channel '{command.Channel}'.");

        var config = NotificationChannelConfig.Create("__test__", command.Channel, command.ConfigJson, true);
        var attempt = NotificationAttempt.CreateEnqueued(Guid.Empty, config.Id, command.Channel, "TestEvent", TestPayload);

        var result = await provider.SendAsync(attempt, config, cancellationToken);

        return new TestNotificationChannelConfigResult(result.Success, result.Response, result.ErrorMessage);
    }
}
