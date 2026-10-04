using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.CustomActions;
using Haven.Application.Features.CustomActions.Queries.GetCustomAction;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.CustomActions;

public sealed class GetCustomActionEndpoint(IMediator mediator)
    : Endpoint<GetCustomActionQuery, ApiResponse<CustomActionDto>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/actions/{actionId}");

        Options(x => x.WithTags("Custom Actions"));
        Summary(s =>
        {
            s.Summary = "Get a custom action";
            s.Description = "Returns a custom action by ID.";
            s[200] = "OK";
            s[404] = "Service or action not found";
        });
    }

    public override async Task HandleAsync(GetCustomActionQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
