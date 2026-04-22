using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.DeployService;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class DeployServiceEndpoint(IMediator mediator)
    : Endpoint<DeployServiceCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/deploy");
        AllowAnonymous();
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Deploy a service";
            s.Description = "Deploys a service to its environment.";
            s[200] = "Success";
            s[400] = "Validation error";
            s[404] = "Service not found";
        });
    }

    public override async Task HandleAsync(DeployServiceCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");
        req.ServiceId = Route<Guid>("serviceId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
