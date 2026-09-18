using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.ExportServiceToDockerCompose;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class ExportServiceToDockerComposeEndpoint(IMediator mediator)
    : Endpoint<ExportServiceToDockerComposeCommand, ApiResponse<string>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/export/docker-compose");

        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Export service to Docker Compose";
            s.Description = "Exports a service as a Docker Compose file.";
            s[200] = "OK";
            s[400] = "Service can't be represented in Compose (e.g. a raw-content Dockerfile)";
            s[404] = "Service not found";
        });
    }

    public override async Task HandleAsync(ExportServiceToDockerComposeCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
