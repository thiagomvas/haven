using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.NotificationChannels.Commands.UpdateNotificationChannelConfig;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class UpdateNotificationChannelConfigEndpoint(IMediator mediator)
    : Endpoint<UpdateNotificationChannelConfigCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Put("/notifications/channels/{id}");
        AllowAnonymous();
        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Update a notification channel";
            s.Description = "Updates an existing notification channel configuration.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Not found";
        });
    }

    public override async Task HandleAsync(UpdateNotificationChannelConfigCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}