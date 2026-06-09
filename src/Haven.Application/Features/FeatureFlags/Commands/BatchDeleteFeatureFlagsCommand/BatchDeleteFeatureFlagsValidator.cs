using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.FeatureFlags.Commands.BatchDeleteFeatureFlagsCommand;

public class BatchDeleteFeatureFlagsValidator : AbstractValidator<BatchDeleteFeatureFlagsCommand>
{
    public BatchDeleteFeatureFlagsValidator()
    {
        RuleFor(x => x.FlagIds)
            .NotEmpty()
            .WithMessage("At least one feature flag ID is required.");

        RuleForEach(x => x.FlagIds)
            .ValidId();
    }
}