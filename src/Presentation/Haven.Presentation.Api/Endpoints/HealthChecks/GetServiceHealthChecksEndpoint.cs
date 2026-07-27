using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.HealthChecks;
using Haven.Application.Features.HealthChecks.Queries.GetServiceHealthChecksQuery;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.HealthChecks;

public sealed class GetServiceHealthChecksEndpoint(IMediator mediator)
    : Endpoint<GetServiceHealthChecksQuery, ApiResponse<IReadOnlyList<HealthCheckDto>>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/health-checks");

        Options(x => x.WithTags("Health Checks"));
        Summary(s =>
        {
            s.Summary = "List service health checks";
            s.Description = "Returns all health checks configured for a specific service.";
            s[200] = "OK";
            s[404] = "Project, environment, or service not found";
        });
    }

    public override async Task HandleAsync(GetServiceHealthChecksQuery req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");
        req.ServiceId = Route<Guid>("serviceId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
