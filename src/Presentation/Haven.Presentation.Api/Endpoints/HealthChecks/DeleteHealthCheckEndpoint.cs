using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.HealthChecks.Commands.DeleteHealthCheckCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.HealthChecks;

public sealed class DeleteHealthCheckEndpoint(IMediator mediator)
    : Endpoint<DeleteHealthCheckCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/health-checks/{healthCheckId}");

        Options(x => x.WithTags("Health Checks"));
        Summary(s =>
        {
            s.Summary = "Delete a health check";
            s.Description = "Deletes a health check by ID and removes its recurring job.";
            s[204] = "Deleted";
            s[404] = "Health check not found";
        });
    }

    public override async Task HandleAsync(DeleteHealthCheckCommand req, CancellationToken ct)
    {
        req.HealthCheckId = Route<Guid>("healthCheckId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}