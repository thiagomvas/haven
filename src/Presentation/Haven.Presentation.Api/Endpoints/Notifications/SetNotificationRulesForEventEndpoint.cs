using FastEndpoints;

using Haven.Application.Features.NotificationRules.Commands.SetNotificationRulesForEvent;
using Haven.Domain;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class SetNotificationRulesForEventRequest
{
    public IReadOnlyList<Guid> ChannelIds { get; set; } = [];
}

public sealed class SetNotificationRulesForEventEndpoint(IMediator mediator)
    : Endpoint<SetNotificationRulesForEventRequest>
{
    public override void Configure()
    {
        Put("/notifications/rules/{eventType}");
        AllowAnonymous();
        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Set rules for a domain event";
            s.Description = "Replaces notification rules for a specific domain event. Pass scope and scopeId query params to set scoped rules.";
            s[200] = "Updated";
            s[400] = "Validation error";
        });
    }

    public override async Task HandleAsync(SetNotificationRulesForEventRequest req, CancellationToken ct)
    {
        var command = new SetNotificationRulesForEventCommand
        {
            EventType = Route<string>("eventType"),
            ChannelIds = req.ChannelIds,
            Scope = Query<NotificationScope?>("scope", isRequired: false),
            ScopeId = Query<Guid?>("scopeId", isRequired: false),
        };
        var result = await mediator.Send(command, ct);
        await this.SendResultAsync(result, ct);
    }
}