using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.FeatureFlags.Commands.CreateFeatureFlagCommand;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.FeatureFlags;

public sealed class CreateFeatureFlagEndpoint(IMediator mediator)
    : Endpoint<CreateFeatureFlagCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/feature-flags");
        AllowAnonymous();
        Options(x => x.WithTags("Feature Flags"));
        Summary(s =>
        {
            s.Summary = "Create a feature flag";
            s.Description = "Creates a new feature flag for a specific service.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[404] = "Service, environment, or project not found";
        });
    }

    public override async Task HandleAsync(CreateFeatureFlagCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
