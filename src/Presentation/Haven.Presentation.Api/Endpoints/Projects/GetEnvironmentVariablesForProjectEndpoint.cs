using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.EnvironmentVariables.Queries.GetEnvFileForProject;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public class GetEnvironmentVariablesForProjectEndpoint(IMediator mediator) : Endpoint<GetEnvFileForProjectQuery, ApiResponse<string>>
{
    public override void Configure()
    {
        Get("/projects/{ProjectId}/env");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetEnvFileForProjectQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}