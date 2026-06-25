using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Application.Features.Services.Queries.GetServiceDashboard;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class GetServiceDashboardEndpoint(IMediator mediator)
    : Endpoint<GetServiceDashboardQuery, ApiResponse<ServiceDashboardDto>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/dashboard");

        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Get service dashboard";
            s.Description = "Returns dashboard data for a service including last deployment time.";
            s[200] = "OK";
            s[404] = "Project, environment, or service not found";
        });
    }

    public override async Task HandleAsync(GetServiceDashboardQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}