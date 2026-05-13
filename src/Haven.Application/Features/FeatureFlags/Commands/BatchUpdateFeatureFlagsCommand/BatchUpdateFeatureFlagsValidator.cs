using FluentValidation;
using Haven.Application.Features.FeatureFlags.Commands.UpdateFeatureFlagCommand;

namespace Haven.Application.Features.FeatureFlags.Commands.BatchUpdateFeatureFlagsCommand;

public class BatchUpdateFeatureFlagsValidator : AbstractValidator<BatchUpdateFeatureFlagsCommand>
{
    public BatchUpdateFeatureFlagsValidator(UpdateFeatureFlagValidator updateValidator)
    {
        RuleFor(x => x.Updates)
            .NotEmpty()
            .WithMessage("At least one feature flag update is required.");

        RuleForEach(x => x.Updates)
            .SetValidator(updateValidator);
    }
}
