using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.HealthChecks;
using Haven.Application.Features.HealthChecks.Commands.TestHealthCheckCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.HealthChecks;

public sealed class TestHealthCheckEndpoint(IMediator mediator)
    : Endpoint<TestHealthCheckCommand, ApiResponse<HealthCheckResultDto>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/health-checks/test");

        Options(x => x.WithTags("Health Checks"));
        Summary(s =>
        {
            s.Summary = "Test a health check configuration";
            s.Description = "Runs an unsaved health check configuration once against the service and returns the outcome. Nothing is persisted.";
            s[200] = "The result of the test run";
            s[400] = "Validation error";
            s[404] = "Service not found";
        });
    }

    public override async Task HandleAsync(TestHealthCheckCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
