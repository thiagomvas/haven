using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Environments.Commands.UpdateEnvironment;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public sealed class UpdateEnvironmentEndpoint(IMediator mediator) : Endpoint<UpdateEnvironmentCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Patch("/projects/{projectId}/environments/{environmentId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateEnvironmentCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
