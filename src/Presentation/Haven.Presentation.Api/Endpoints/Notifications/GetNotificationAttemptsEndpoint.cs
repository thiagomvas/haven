using FastEndpoints;

using Haven.Application.Common.Messaging;
using Haven.Application.Features.NotificationChannels;
using Haven.Application.Features.NotificationChannels.Queries.GetNotificationAttempts;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class GetNotificationAttemptsEndpoint(IMediator mediator)
    : Endpoint<GetNotificationAttemptsQuery, PagedResult<NotificationAttemptDto>>
{
    public override void Configure()
    {
        Get("/notifications/channels/{channelConfigId}/attempts");
        AllowAnonymous();
        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Get delivery attempts for a channel";
            s.Description = "Returns a paginated list of notification delivery attempts for the specified channel configuration.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(GetNotificationAttemptsQuery req, CancellationToken ct)
    {
        req.ChannelConfigId = Route<Guid>("channelConfigId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}