using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.NotificationChannels.Commands.CreateNotificationChannelConfig;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class CreateNotificationChannelConfigEndpoint(IMediator mediator)
    : Endpoint<CreateNotificationChannelConfigCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/notifications/channels");
        
        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Create a notification channel";
            s.Description = "Creates a new notification channel configuration.";
            s[201] = "Created";
            s[400] = "Validation error";
        });
    }

    public override async Task HandleAsync(CreateNotificationChannelConfigCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}