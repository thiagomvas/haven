using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForProject;
using Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForService;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public class GetEnvironmentVariablesForProjectEndpoint(IMediator mediator) : Endpoint<GetEnvFileForServiceQuery, ApiResponse<string>>
{
    public override void Configure()
    {
        Get("/projects/{ProjectId}/environments/{EnvironmentId}/services/{ServiceId}/env");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetEnvFileForServiceQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}