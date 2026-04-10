using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Projects.Commands.CreateProject;
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
        var response = ApiResponse<Guid>.FromResult(result);

        if (result.IsSuccess)
            await Send.CreatedAtAsync("/api/projects/{0}", response.Data, response, cancellation: ct);
        else
            await Send.ResponseAsync(response, StatusCodes.Status400BadRequest, cancellation: ct);
    }
}
