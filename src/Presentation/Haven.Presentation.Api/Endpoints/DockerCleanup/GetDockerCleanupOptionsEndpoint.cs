using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Configuration;
using Haven.Application.Features.DockerCleanup.Queries.GetDockerCleanupOptions;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.DockerCleanup;

public sealed class GetDockerCleanupOptionsEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<DockerCleanupOptions>>
{
    public override void Configure()
    {
        Get("/docker-cleanup/options");
        Options(x => x.WithTags("DockerCleanup"));
        Summary(s =>
        {
            s.Summary = "Get Docker cleanup options";
            s.Description = "Returns the current configuration for the orphaned container/image cleanup job.";
            s[200] = "Success";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetDockerCleanupOptionsQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}