using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.CreateService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class CreateServiceEndpoint(IMediator mediator)
    : Endpoint<CreateServiceCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services");
        AllowAnonymous();
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Create a service";
            s.Description = "Creates a new service within an environment and returns its ID.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[404] = "Environment not found";
        });
    }

    public override async Task HandleAsync(CreateServiceCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}