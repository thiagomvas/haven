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

        Options(x => x.WithTags("Environment Variables"));
        Summary(s =>
        {
            s.Summary = "Set environment variables for a project";
            s.Description = "Replaces the project-level variables with the provided .env file content (EnvFile).";
            s[200] = "Variables updated";
            s[400] = "Validation error";
            s[404] = "Project not found";
        });
    }

    public override async Task HandleAsync(SetEnvForProjectCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}