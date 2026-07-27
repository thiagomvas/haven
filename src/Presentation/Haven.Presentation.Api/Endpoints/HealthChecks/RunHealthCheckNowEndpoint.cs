using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.HealthChecks.Commands.RunHealthCheckNowCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.HealthChecks;

public sealed class RunHealthCheckNowEndpoint(IMediator mediator)
    : Endpoint<RunHealthCheckNowCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/health-checks/{healthCheckId}/run");

        Options(x => x.WithTags("Health Checks"));
        Summary(s =>
        {
            s.Summary = "Run a health check now";
            s.Description = "Enqueues an immediate one-off run of the health check, independent of its recurring schedule.";
            s[200] = "Enqueued";
            s[404] = "Health check not found";
        });
    }

    public override async Task HandleAsync(RunHealthCheckNowCommand req, CancellationToken ct)
    {
        req.HealthCheckId = Route<Guid>("healthCheckId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
