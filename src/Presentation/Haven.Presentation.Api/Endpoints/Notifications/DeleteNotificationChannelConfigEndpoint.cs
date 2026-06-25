using FastEndpoints;

using Haven.Application.Features.NotificationChannels.Commands.DeleteNotificationChannelConfig;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class DeleteNotificationChannelConfigEndpoint(IMediator mediator) : Endpoint<DeleteNotificationChannelConfigCommand>
{
    public override void Configure()
    {
        Delete("/notifications/channels/{id}");

        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Delete a notification channel";
            s.Description = "Permanently deletes a notification channel configuration by ID.";
            s[204] = "Deleted";
            s[404] = "Not found";
        });
    }

    public override async Task HandleAsync(DeleteNotificationChannelConfigCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}