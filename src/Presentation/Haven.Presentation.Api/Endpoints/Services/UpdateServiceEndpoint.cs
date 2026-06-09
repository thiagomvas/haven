using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.UpdateService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class UpdateServiceEndpoint(IMediator mediator)
    : Endpoint<UpdateServiceCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Patch("/projects/{projectId}/environments/{environmentId}/services/{serviceId}");
        AllowAnonymous();
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Update a service";
            s.Description = "Partially updates a service within an environment and returns its ID.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Service, environment, or project not found";
            s[409] = "Service name conflict";
        });
    }

    public override async Task HandleAsync(UpdateServiceCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");
        req.ServiceId = Route<Guid>("serviceId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}