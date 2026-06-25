using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForEnvironment;
using Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForProject;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public class GetEnvironmentVariablesForEnvironmentEndpoint(IMediator mediator) : Endpoint<GetEnvFileForEnvironmentQuery, ApiResponse<string>>
{
    public override void Configure()
    {
        Get("/projects/{ProjectId}/environments/{EnvironmentId}/env");

    }

    public override async Task HandleAsync(GetEnvFileForEnvironmentQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}