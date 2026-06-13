using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.NotificationRules;
using Haven.Application.Features.NotificationRules.Queries.GetAllNotificationRules;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class GetAllNotificationRulesEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<NotificationRuleEventConfigDto[]>>
{
    public override void Configure()
    {
        Get("/notifications/rules");
        AllowAnonymous();
        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Get all notification rules";
            s.Description = "Returns channel assignments for all domain event types in a single request.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllNotificationRulesQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}
