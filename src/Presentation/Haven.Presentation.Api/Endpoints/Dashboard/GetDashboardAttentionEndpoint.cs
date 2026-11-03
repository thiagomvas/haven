using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Dashboard.Queries.GetDashboardAttention;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Dashboard;

public sealed class GetDashboardAttentionEndpoint(IMediator mediator)
    : Endpoint<GetDashboardAttentionQuery, ApiResponse<List<AttentionServiceDto>>>
{
    public override void Configure()
    {
        Get("/dashboard/attention");
        Options(x => x.WithTags("Dashboard"));
        Summary(s =>
        {
            s.Summary = "Get services needing attention";
            s.Description = "Returns services that are unhealthy, crashed, or whose latest deployment failed, most severe and most recent first.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(GetDashboardAttentionQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
