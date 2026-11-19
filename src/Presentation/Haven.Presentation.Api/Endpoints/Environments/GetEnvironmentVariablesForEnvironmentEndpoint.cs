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

        Options(x => x.WithTags("Environment Variables"));
        Summary(s =>
        {
            s.Summary = "Get environment variables for an environment";
            s.Description = "Returns the environment-level variables as raw .env file content.";
            s[200] = "Env file content";
            s[404] = "Project or environment not found";
        });
    }

    public override async Task HandleAsync(GetEnvFileForEnvironmentQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}