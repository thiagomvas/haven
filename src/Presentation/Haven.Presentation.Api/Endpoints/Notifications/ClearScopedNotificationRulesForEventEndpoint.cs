using FastEndpoints;

using Haven.Application.Features.NotificationRules.Commands.ClearScopedNotificationRulesForEvent;
using Haven.Domain;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Notifications;

public sealed class ClearScopedNotificationRulesForEventEndpoint(IMediator mediator)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/notifications/rules/{eventType}");
        AllowAnonymous();
        Options(x => x.WithTags("Notifications"));
        Summary(s =>
        {
            s.Summary = "Clear scoped notification rules for a domain event";
            s.Description = "Removes all scoped overrides for a specific domain event, reverting to global defaults.";
            s[204] = "Cleared";
            s[400] = "Validation error";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var scope = Query<NotificationScope?>("scope", isRequired: false);
        var scopeId = Query<Guid?>("scopeId", isRequired: false);

        if (scope is null || scopeId is null)
        {
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var command = new ClearScopedNotificationRulesForEventCommand
        {
            EventType = Route<string>("eventType"),
            Scope = scope.Value,
            ScopeId = scopeId.Value,
        };
        var result = await mediator.Send(command, ct);
        await this.SendResultAsync(result, ct);
    }
}
