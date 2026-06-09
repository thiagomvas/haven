using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.FeatureFlags.Commands.BatchUpdateFeatureFlagsCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.FeatureFlags;

public sealed class BatchUpdateFeatureFlagsEndpoint(IMediator mediator)
    : Endpoint<BatchUpdateFeatureFlagsCommand, ApiResponse<IReadOnlyList<Guid>>>
{
    public override void Configure()
    {
        Patch("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/feature-flags/batch");
        AllowAnonymous();
        Options(x => x.WithTags("Feature Flags"));
        Summary(s =>
        {
            s.Summary = "Batch update feature flags";
            s.Description = "Updates multiple feature flags for a specific service in a single request.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Feature flag, service, environment, or project not found";
        });
    }

    public override async Task HandleAsync(BatchUpdateFeatureFlagsCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}