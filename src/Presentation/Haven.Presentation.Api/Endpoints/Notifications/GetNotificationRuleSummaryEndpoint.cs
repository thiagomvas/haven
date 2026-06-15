using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.NotificationRules;
using Haven.Application.Features.NotificationRules.Queries.GetNotificationRuleSummary;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class GetNotificationRuleSummaryEndpoint(IMediator mediator)
    : Endpoint<GetNotificationRuleSummaryQuery, ApiResponse<NotificationRuleSummaryItemDto[]>>
{
    public override void Configure()
    {
        Get("/notifications/rules/summary");
        AllowAnonymous();
        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Get notification rule summary";
            s.Description = "Returns all domain event types with their configured rule counts. Pass scope and scopeId to get scoped summary.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(GetNotificationRuleSummaryQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
