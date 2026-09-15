using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Environments.Commands.ExportEnvironmentToDockerCompose;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public sealed class ExportEnvironmentToDockerComposeEndpoint(IMediator mediator)
    : Endpoint<ExportEnvironmentToDockerComposeCommand, ApiResponse<string>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/export/docker-compose");

        Options(x => x.WithTags("Environments"));
        Summary(s =>
        {
            s.Summary = "Export environment to Docker Compose";
            s.Description = "Exports the selected services of an environment as a single Docker Compose file.";
            s[200] = "OK";
            s[404] = "Environment not found";
        });
    }

    public override async Task HandleAsync(ExportEnvironmentToDockerComposeCommand req, CancellationToken ct)
    {
        req.EnvironmentId = Route<Guid>("environmentId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
