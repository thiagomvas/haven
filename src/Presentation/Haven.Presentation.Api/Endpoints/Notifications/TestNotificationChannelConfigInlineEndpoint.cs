using FastEndpoints;

using Haven.Application.Features.NotificationChannels.Commands.TestNotificationChannelConfig;
using Haven.Application.Features.NotificationChannels.Commands.TestNotificationChannelConfigInline;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class TestNotificationChannelConfigInlineEndpoint(IMediator mediator)
    : Endpoint<TestNotificationChannelConfigInlineCommand, TestNotificationChannelConfigResult>
{
    public override void Configure()
    {
        Post("/notifications/channels/test");
        AllowAnonymous();
        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Test a notification channel config inline";
            s.Description = "Sends a test payload using the provided config without persisting anything.";
            s[200] = "Test result returned";
        });
    }

    public override async Task HandleAsync(TestNotificationChannelConfigInlineCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
