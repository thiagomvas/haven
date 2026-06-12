using FastEndpoints;

using Haven.Application.Features.NotificationChannels.Commands.SetNotificationChannelEnabled;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class SetNotificationChannelEnabledEndpoint(IMediator mediator) : Endpoint<SetNotificationChannelEnabledCommand>
{
    public override void Configure()
    {
        Patch("/notification-channels/{id}/enabled");
        AllowAnonymous();
        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Set notification channel enabled state";
            s.Description = "Enables or disables a notification channel.";
            s[204] = "Updated";
            s[404] = "Not found";
        });
    }

    public override async Task HandleAsync(SetNotificationChannelEnabledCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
