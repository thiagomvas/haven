using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.CustomActions.Commands.DeleteCustomAction;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.CustomActions;

public sealed class DeleteCustomActionEndpoint(IMediator mediator)
    : Endpoint<DeleteCustomActionCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/actions/{actionId}");

        Options(x => x.WithTags("Custom Actions"));
        Summary(s =>
        {
            s.Summary = "Delete a custom action";
            s.Description = "Deletes a custom action from a service.";
            s[200] = "Deleted";
            s[404] = "Service or action not found";
        });
    }

    public override async Task HandleAsync(DeleteCustomActionCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.ActionId = Route<Guid>("actionId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
