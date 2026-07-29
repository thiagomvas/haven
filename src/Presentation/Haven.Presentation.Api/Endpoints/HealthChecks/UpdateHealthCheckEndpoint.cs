using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.HealthChecks.Commands.UpdateHealthCheckCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.HealthChecks;

public sealed class UpdateHealthCheckEndpoint(IMediator mediator)
    : Endpoint<UpdateHealthCheckCommand, ApiResponse>
{
    public override void Configure()
    {
        Patch("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/health-checks/{healthCheckId}");

        Options(x => x.WithTags("Health Checks"));
        Summary(s =>
        {
            s.Summary = "Update a health check";
            s.Description = "Partially updates a health check and reschedules its recurring job to match.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Health check, service, environment, or project not found";
        });
    }

    public override async Task HandleAsync(UpdateHealthCheckCommand req, CancellationToken ct)
    {
        req.HealthCheckId = Route<Guid>("healthCheckId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}