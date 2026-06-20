using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Environments.Queries.GetEnvironmentDashboard;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public sealed class GetEnvironmentDashboardEndpoint(IMediator mediator)
    : Endpoint<GetEnvironmentDashboardQuery, ApiResponse<EnvironmentDashboardDto>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/dashboard");
        Options(x => x.WithTags("Environments"));
        Summary(s =>
        {
            s.Summary = "Get environment dashboard";
            s.Description = "Returns dashboard data for a specific environment, including service status, health information, and environment variables.";
            s[200] = "OK";
            s[404] = "Project or environment not found";
        });
    }

    public override async Task HandleAsync(GetEnvironmentDashboardQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}