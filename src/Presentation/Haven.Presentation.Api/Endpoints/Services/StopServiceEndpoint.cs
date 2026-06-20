using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.DeployService;
using Haven.Application.Features.Services.Commands.StopService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;


public sealed class StopServiceEndpoint(IMediator mediator)
    : Endpoint<StopServiceCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/stop");
        AllowAnonymous();
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Stops a service";
            s.Description = "Stops a service in its environment.";
            s[200] = "Success";
            s[400] = "Validation error";
            s[404] = "Service not found";
        });
    }

    public override async Task HandleAsync(StopServiceCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");
        req.ServiceId = Route<Guid>("serviceId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}