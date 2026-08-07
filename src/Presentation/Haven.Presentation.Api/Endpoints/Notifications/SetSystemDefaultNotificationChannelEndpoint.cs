using FastEndpoints;

using Haven.Application.Features.NotificationChannels.Commands.SetSystemDefaultNotificationChannel;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class SetSystemDefaultNotificationChannelEndpoint(IMediator mediator) : Endpoint<SetSystemDefaultNotificationChannelCommand>
{
    public override void Configure()
    {
        Patch("/notifications/channels/{id}/system-default");

        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Set the system default SMTP provider";
            s.Description = "Marks this SMTP channel config as the one used to send transactional/system emails (invites, password recovery). Clears the flag on any other SMTP config.";
            s[204] = "Updated";
            s[400] = "Only SMTP channels can be marked as the system default";
            s[404] = "Not found";
        });
    }

    public override async Task HandleAsync(SetSystemDefaultNotificationChannelCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}