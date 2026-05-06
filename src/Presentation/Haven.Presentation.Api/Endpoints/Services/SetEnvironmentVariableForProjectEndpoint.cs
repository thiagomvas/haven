using FastEndpoints;
using Haven.Application.Common;
using Haven.Application.Common.Responses;
using Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;
using Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForService;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public class SetEnvironmentVariableForServiceEndpoint(IMediator mediator) : Endpoint<SetEnvForServiceCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/projects/{ProjectId}/environments/{EnvironmentId}/services/{ServiceId}/env");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SetEnvForServiceCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}