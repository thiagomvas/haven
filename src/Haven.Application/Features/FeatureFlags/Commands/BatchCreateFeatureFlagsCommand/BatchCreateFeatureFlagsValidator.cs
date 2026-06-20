using FluentValidation;

using Haven.Application.Features.FeatureFlags.Commands.CreateFeatureFlagCommand;

namespace Haven.Application.Features.FeatureFlags.Commands.BatchCreateFeatureFlagsCommand;

public class BatchCreateFeatureFlagsValidator : AbstractValidator<BatchCreateFeatureFlagsCommand>
{
    public BatchCreateFeatureFlagsValidator(CreateFeatureFlagValidator createValidator)
    {
        RuleFor(x => x.Creates)
            .NotEmpty()
            .WithMessage("At least one feature flag creation is required.");

        RuleForEach(x => x.Creates)
            .SetValidator(createValidator);
    }
}