using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.CloneService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class CloneServiceEndpoint(IMediator mediator) : Endpoint<CloneServiceCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/clone");
        AllowAnonymous();
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Clone a service";
            s.Description = "Creates an exact copy of a service including its configuration, environment variables, and feature flags. Can optionally clone into a different environment within the same project.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[404] = "Project, environment, or service not found";
            s[409] = "A service with the new name already exists in the target environment";
        });
    }

    public override async Task HandleAsync(CloneServiceCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");
        req.ServiceId = Route<Guid>("serviceId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}