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

        Options(x => x.WithTags("Environment Variables"));
        Summary(s =>
        {
            s.Summary = "Get environment variables for a project";
            s.Description = "Returns the project-level variables as raw .env file content.";
            s[200] = "Env file content";
            s[404] = "Project not found";
        });
    }

    public override async Task HandleAsync(GetEnvFileForProjectQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}