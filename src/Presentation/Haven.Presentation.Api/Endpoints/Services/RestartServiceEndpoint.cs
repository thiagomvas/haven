using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.RestartService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class RestartServiceEndpoint(IMediator mediator)
    : Endpoint<RestartServiceCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/restart");
        AllowAnonymous();
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Restart a service";
            s.Description = "Restarts a service with new environment variables and configuration. Does not repull the Docker image.";
            s[200] = "Success";
            s[400] = "Validation error";
            s[404] = "Service not found";
        });
    }

    public override async Task HandleAsync(RestartServiceCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");
        req.ServiceId = Route<Guid>("serviceId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}