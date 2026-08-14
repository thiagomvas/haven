using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Dashboard.Queries.GetDashboardOverview;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Dashboard;

public sealed class GetDashboardOverviewEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<DashboardOverviewDto>>
{
    public override void Configure()
    {
        Get("/dashboard/overview");
        Options(x => x.WithTags("Dashboard"));
        Summary(s =>
        {
            s.Summary = "Get dashboard overview";
            s.Description = "Returns system-wide health metrics: total projects/environments, aggregate service status breakdown, the environment most in need of attention (if any), and recent deployment activity.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetDashboardOverviewQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}
