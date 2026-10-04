using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.CustomActions.Commands.ExecuteCustomAction;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.CustomActions;

public sealed class ExecuteCustomActionEndpoint(IMediator mediator)
    : Endpoint<ExecuteCustomActionCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/actions/{actionId}/execute");

        Options(x => x.WithTags("Custom Actions"));
        Summary(s =>
        {
            s.Summary = "Execute a custom action";
            s.Description = "Runs a custom action against its service.";
            s[200] = "Executed";
            s[404] = "Service or action not found";
        });
    }

    public override async Task HandleAsync(ExecuteCustomActionCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.ActionId = Route<Guid>("actionId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
