using FastEndpoints;

using Haven.Application.Common;
using Haven.Application.Common.Responses;
using Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public class SetEnvironmentVariableForProjectEndpoint(IMediator mediator) : Endpoint<SetEnvForProjectCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/projects/{ProjectId}/env");
    }

    public override async Task HandleAsync(SetEnvForProjectCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}