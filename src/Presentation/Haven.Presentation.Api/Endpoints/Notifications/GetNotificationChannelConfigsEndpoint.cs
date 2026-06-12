using FastEndpoints;

using Haven.Application.Common.Messaging;
using Haven.Application.Features.NotificationChannels;
using Haven.Application.Features.NotificationChannels.Queries.GetNotificationChannelConfigs;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class GetNotificationChannelConfigsEndpoint(IMediator mediator)
    : Endpoint<GetNotificationChannelConfigsQuery, PagedResult<NotificationChannelConfigDto>>
{
    public override void Configure()
    {
        Get("/notification-channels");
        AllowAnonymous();
        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "List notification channels";
            s.Description = "Returns a paginated list of notification channel configurations.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(GetNotificationChannelConfigsQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
