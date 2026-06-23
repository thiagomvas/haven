using FastEndpoints;

using Haven.Application.Features.NotificationChannels.Commands.TestNotificationChannelConfig;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class TestNotificationChannelConfigEndpoint(IMediator mediator)
    : Endpoint<TestNotificationChannelConfigCommand, TestNotificationChannelConfigResult>
{
    public override void Configure()
    {
        Post("/notifications/channels/{id}/test");
        
        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Test a notification channel";
            s.Description = "Sends a test payload to the channel and returns the result.";
            s[200] = "Test result returned";
            s[404] = "Not found";
        });
    }

    public override async Task HandleAsync(TestNotificationChannelConfigCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}