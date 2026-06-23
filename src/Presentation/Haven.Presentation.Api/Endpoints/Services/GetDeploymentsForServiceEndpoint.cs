using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Deployments;
using Haven.Application.Features.Deployments.Queries.GetDeploymentsForService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class GetDeploymentsForServiceEndpoint(IMediator mediator)
    : Endpoint<GetDeploymentsForServiceQuery, ApiResponse<List<DeploymentDto>>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/deployments");
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Get deployments for service";
            s.Description = "Returns all deployments for a service ordered by start time descending.";
            s[200] = "OK";
            s[404] = "Project, environment, or service not found";
        });
    }

    public override async Task HandleAsync(GetDeploymentsForServiceQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}