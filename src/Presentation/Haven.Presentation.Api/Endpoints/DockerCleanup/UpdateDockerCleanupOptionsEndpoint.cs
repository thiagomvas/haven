using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Configuration;
using Haven.Application.Features.DockerCleanup.Commands.UpdateDockerCleanupOptions;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.DockerCleanup;

public sealed class UpdateDockerCleanupOptionsEndpoint(IMediator mediator)
    : Endpoint<UpdateDockerCleanupOptionsCommand, ApiResponse<DockerCleanupOptions>>
{
    public override void Configure()
    {
        Put("/docker-cleanup/options");
        Options(x => x.WithTags("DockerCleanup"));
        Summary(s =>
        {
            s.Summary = "Update Docker cleanup options";
            s.Description = "Persists the orphaned container/image cleanup job configuration to the database.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(UpdateDockerCleanupOptionsCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
