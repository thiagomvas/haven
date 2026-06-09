using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Environments.Commands.CreateEnvironment;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public class CreateEnvironmentEndpoint : Endpoint<CreateEnvironmentCommand, ApiResponse<Guid>>
{
    private readonly IMediator _mediator;

    public CreateEnvironmentEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Post("/projects/{projectId}/environments");
        AllowAnonymous();
        Options(x => x.WithTags("Environments"));
        Summary(s =>
        {
            s.Summary = "Create an environment";
            s.Description = "Creates a new environment within a project and returns its ID.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[404] = "Project not found";
        });
    }

    public override async Task HandleAsync(CreateEnvironmentCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        var result = await _mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}