using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.FeatureFlags.Commands.UpdateFeatureFlagCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.FeatureFlags;

public sealed class UpdateFeatureFlagEndpoint(IMediator mediator)
    : Endpoint<UpdateFeatureFlagCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Patch("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/feature-flags/{flagId}");
        
        Options(x => x.WithTags("Feature Flags"));
        Summary(s =>
        {
            s.Summary = "Update a feature flag";
            s.Description = "Partially updates a feature flag for a specific service.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Feature flag, service, environment, or project not found";
        });
    }

    public override async Task HandleAsync(UpdateFeatureFlagCommand req, CancellationToken ct)
    {
        req.FlagId = Route<Guid>("flagId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}