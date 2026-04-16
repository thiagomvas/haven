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
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateProjectCommand req, CancellationToken ct)
    {
        var result = await _mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
