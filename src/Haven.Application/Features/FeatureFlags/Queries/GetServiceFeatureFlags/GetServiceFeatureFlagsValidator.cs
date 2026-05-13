using FluentValidation;
using Haven.Application.Extensions;

namespace Haven.Application.Features.FeatureFlags.Queries.GetServiceFeatureFlags;

public class GetServiceFeatureFlagsValidator : AbstractValidator<GetServiceFeatureFlagsQuery>
{
    public GetServiceFeatureFlagsValidator()
    {
        RuleFor(x => x.ProjectId)
            .ValidId();
        
        RuleFor(x => x.ServiceId)
            .ValidId();

        RuleFor(x => x.EnvironmentId)
            .ValidId();
    }
}