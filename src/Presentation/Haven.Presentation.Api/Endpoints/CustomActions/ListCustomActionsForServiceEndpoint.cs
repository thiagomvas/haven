using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.CustomActions;
using Haven.Application.Features.CustomActions.Queries.ListCustomActionsForService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.CustomActions;

public sealed class ListCustomActionsForServiceEndpoint(IMediator mediator)
    : Endpoint<ListCustomActionsForServiceQuery, ApiResponse<IReadOnlyList<CustomActionDto>>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/actions");

        Options(x => x.WithTags("Custom Actions"));
        Summary(s =>
        {
            s.Summary = "List custom actions";
            s.Description = "Returns all custom actions of a service.";
            s[200] = "OK";
            s[404] = "Service not found";
        });
    }

    public override async Task HandleAsync(ListCustomActionsForServiceQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
