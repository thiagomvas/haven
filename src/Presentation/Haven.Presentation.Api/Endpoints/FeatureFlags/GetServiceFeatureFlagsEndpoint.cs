using FastEndpoints;

using Haven.Application.Common.Messaging;
using Haven.Application.Features.FeatureFlags;
using Haven.Application.Features.FeatureFlags.Queries.GetServiceFeatureFlags;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.FeatureFlags;

public sealed class GetServiceFeatureFlagsEndpoint(IMediator mediator)
    : Endpoint<GetServiceFeatureFlagsQuery, PagedResult<FeatureFlagDto>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/feature-flags");
        
        Options(x => x.WithTags("Feature Flags"));
        Summary(s =>
        {
            s.Summary = "List service feature flags";
            s.Description = "Returns a paginated list of feature flags for a specific service.";
            s[200] = "OK";
            s[404] = "Project, environment, or service not found";
        });
    }

    public override async Task HandleAsync(GetServiceFeatureFlagsQuery req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");
        req.ServiceId = Route<Guid>("serviceId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}