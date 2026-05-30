using FastEndpoints;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public sealed class GetProjectsDashboardEndpoint(IMediator mediator)
    : Endpoint<GetProjectsDashboardQuery, PagedResult<ProjectDashboardDto>>
{
    public override void Configure()
    {
        Get("/projects/dashboard");
        Options(x => x.WithTags("Projects"));
        Summary(s =>
        {
            s.Summary = "Get projects dashboard";
            s.Description = "Returns a paginated list of projects with dashboard data including per-environment service counts and last deployment timestamp.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(GetProjectsDashboardQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
