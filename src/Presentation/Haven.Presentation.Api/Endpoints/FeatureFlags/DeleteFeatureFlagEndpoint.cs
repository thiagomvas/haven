using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.FeatureFlags.Commands.DeleteFeatureFlagCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.FeatureFlags;

public sealed class DeleteFeatureFlagEndpoint(IMediator mediator)
    : Endpoint<DeleteFeatureFlagCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/feature-flags/{flagId}");

        Options(x => x.WithTags("Feature Flags"));
        Summary(s =>
        {
            s.Summary = "Delete a feature flag";
            s.Description = "Deletes a feature flag by ID.";
            s[204] = "Deleted";
            s[404] = "Feature flag not found";
        });
    }

    public override async Task HandleAsync(DeleteFeatureFlagCommand req, CancellationToken ct)
    {
        req.FlagId = Route<Guid>("flagId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}