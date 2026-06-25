using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.FeatureFlags.Commands.BatchDeleteFeatureFlagsCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.FeatureFlags;

public sealed class BatchDeleteFeatureFlagsEndpoint(IMediator mediator)
    : Endpoint<BatchDeleteFeatureFlagsCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/feature-flags/batch");

        Options(x => x.WithTags("Feature Flags"));
        Summary(s =>
        {
            s.Summary = "Batch delete feature flags";
            s.Description = "Deletes multiple feature flags in a single request.";
            s[204] = "Deleted";
            s[404] = "One or more feature flags not found";
        });
    }

    public override async Task HandleAsync(BatchDeleteFeatureFlagsCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}