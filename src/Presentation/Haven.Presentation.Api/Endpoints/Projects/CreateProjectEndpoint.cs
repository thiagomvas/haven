using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Projects.Commands.CreateProject;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public class CreateProjectEndpoint : Endpoint<CreateProjectCommand, ApiResponse<Guid>>
{
    private readonly IMediator _mediator;

    public CreateProjectEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Post("/projects");
        Options(x => x.WithTags("Projects"));
        Summary(s =>
        {
            s.Summary = "Create a project";
            s.Description = "Creates a new project and returns its ID.";
            s[201] = "Created";
            s[400] = "Validation error";
        });
    }

    public override async Task HandleAsync(CreateProjectCommand req, CancellationToken ct)
    {
        var result = await _mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}