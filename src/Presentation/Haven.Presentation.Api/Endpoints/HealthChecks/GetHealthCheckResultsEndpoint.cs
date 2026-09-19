using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.HealthChecks;
using Haven.Application.Features.HealthChecks.Queries.GetHealthCheckResultsQuery;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.HealthChecks;

public sealed class GetHealthCheckResultsEndpoint(IMediator mediator)
    : Endpoint<GetHealthCheckResultsQuery, ApiResponse<IReadOnlyList<HealthCheckResultDto>>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/health-checks/{healthCheckId}/results");

        Options(x => x.WithTags("Health Checks"));
        Summary(s =>
        {
            s.Summary = "List health check results";
            s.Description = "Returns the most recent runs of a health check, newest first.";
            s[200] = "OK";
            s[404] = "Health check not found";
        });
    }

    public override async Task HandleAsync(GetHealthCheckResultsQuery req, CancellationToken ct)
    {
        req.HealthCheckId = Route<Guid>("healthCheckId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
