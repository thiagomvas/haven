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
    }

    public override async Task HandleAsync(CreateEnvironmentCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        var result = await _mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
