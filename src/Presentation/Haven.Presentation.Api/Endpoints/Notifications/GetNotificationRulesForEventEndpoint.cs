using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.NotificationRules;
using Haven.Application.Features.NotificationRules.Queries.GetNotificationRulesForEvent;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class GetNotificationRulesForEventEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<NotificationRuleEventConfigDto>>
{
    public override void Configure()
    {
        Get("/notifications/rules/{eventType}");
        AllowAnonymous();
        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Get rules for a domain event";
            s.Description = "Returns the notification channels configured for a specific domain event.";
            s[200] = "OK";
            s[400] = "Validation error";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var query = new GetNotificationRulesForEventQuery
        {
            EventType = Route<string>("eventType"),
        };
        var result = await mediator.Send(query, ct);
        await this.SendResultAsync(result, ct);
    }
}
