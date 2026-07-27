using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.HealthChecks.Commands.CreateHealthCheckCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.HealthChecks;

public sealed class CreateHealthCheckEndpoint(IMediator mediator)
    : Endpoint<CreateHealthCheckCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/health-checks");

        Options(x => x.WithTags("Health Checks"));
        Summary(s =>
        {
            s.Summary = "Create a health check";
            s.Description = "Creates a new health check for a specific service and schedules its recurring job.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[404] = "Service, environment, or project not found";
        });
    }

    public override async Task HandleAsync(CreateHealthCheckCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
