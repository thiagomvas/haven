using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Projects.Queries.GetProjectDashboard;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public sealed class GetProjectDashboardEndpoint(IMediator mediator)
    : Endpoint<GetProjectDashboardQuery, ApiResponse<ProjectDashboardDto>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/dashboard");
        AllowAnonymous();
        Options(x => x.WithTags("Projects"));
        Summary(s =>
        {
            s.Summary = "Get project dashboard";
            s.Description = "Returns dashboard data for a specific project, including per-environment service status and health information.";
            s[200] = "OK";
            s[404] = "Project not found";
        });
    }

    public override async Task HandleAsync(GetProjectDashboardQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
