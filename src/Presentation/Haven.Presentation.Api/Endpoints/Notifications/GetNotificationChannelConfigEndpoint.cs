using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.NotificationChannels;
using Haven.Application.Features.NotificationChannels.Queries.GetNotificationChannelConfig;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class GetNotificationChannelConfigEndpoint(IMediator mediator)
    : Endpoint<GetNotificationChannelConfigQuery, ApiResponse<NotificationChannelConfigDto>>
{
    public override void Configure()
    {
        Get("/notifications/channels/{id}");
        AllowAnonymous();
        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Get a notification channel";
            s.Description = "Returns a notification channel configuration by ID.";
            s[200] = "OK";
            s[404] = "Not found";
        });
    }

    public override async Task HandleAsync(GetNotificationChannelConfigQuery req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}