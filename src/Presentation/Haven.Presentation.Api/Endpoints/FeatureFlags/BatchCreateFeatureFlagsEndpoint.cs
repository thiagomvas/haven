using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.FeatureFlags.Commands.BatchCreateFeatureFlagsCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.FeatureFlags;

public sealed class BatchCreateFeatureFlagsEndpoint(IMediator mediator)
    : Endpoint<BatchCreateFeatureFlagsCommand, ApiResponse<IReadOnlyList<Guid>>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/feature-flags/batch");
        
        Options(x => x.WithTags("Feature Flags"));
        Summary(s =>
        {
            s.Summary = "Batch create feature flags";
            s.Description = "Creates multiple feature flags for a specific service in a single request.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[404] = "Service, environment, or project not found";
        });
    }

    public override async Task HandleAsync(BatchCreateFeatureFlagsCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}