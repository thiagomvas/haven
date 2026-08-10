using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Configuration;
using Haven.Application.Features.RepositoryCleanup.Queries.GetRepositoryCleanupOptions;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.RepositoryCleanup;

public sealed class GetRepositoryCleanupOptionsEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<RepositoryCleanupOptions>>
{
    public override void Configure()
    {
        Get("/repository-cleanup/options");
        Options(x => x.WithTags("RepositoryCleanup"));
        Summary(s =>
        {
            s.Summary = "Get repository cleanup options";
            s.Description = "Returns the current configuration for the dangling repository cleanup job.";
            s[200] = "Success";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetRepositoryCleanupOptionsQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}