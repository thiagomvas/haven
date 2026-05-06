using FastEndpoints;
using Haven.Application.Common;
using Haven.Application.Common.Responses;
using Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForEnvironment;
using Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public class SetEnvironmentVariableForEnvironmentEndpoint(IMediator mediator) : Endpoint<SetEnvForEnvironmentCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/projects/{ProjectId}/environments/{EnvironmentId}/env");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SetEnvForEnvironmentCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}