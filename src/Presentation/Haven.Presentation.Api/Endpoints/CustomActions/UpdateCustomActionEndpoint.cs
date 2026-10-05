using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.CustomActions.Commands.UpdateCustomAction;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.CustomActions;

public sealed class UpdateCustomActionEndpoint(IMediator mediator)
    : Endpoint<UpdateCustomActionCommand, ApiResponse>
{
    public override void Configure()
    {
        Patch("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/actions/{actionId}");

        Options(x => x.WithTags("Custom Actions"));
        Summary(s =>
        {
            s.Summary = "Update a custom action";
            s.Description = "Partially updates a custom action.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Service or action not found";
            s[409] = "Action name conflict";
        });
    }

    public override async Task HandleAsync(UpdateCustomActionCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.ActionId = Route<Guid>("actionId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}