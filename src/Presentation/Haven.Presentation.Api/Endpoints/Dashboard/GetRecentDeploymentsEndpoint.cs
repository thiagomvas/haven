using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Deployments.Queries.GetRecentDeployments;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Dashboard;

public sealed class GetRecentDeploymentsEndpoint(IMediator mediator)
    : Endpoint<GetRecentDeploymentsQuery, ApiResponse<List<RecentDeploymentDto>>>
{
    public override void Configure()
    {
        Get("/dashboard/deployments");
        Options(x => x.WithTags("Dashboard"));
        Summary(s =>
        {
            s.Summary = "Get recent deployments";
            s.Description = "Returns the most recent deployments across all services, newest first.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(GetRecentDeploymentsQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}