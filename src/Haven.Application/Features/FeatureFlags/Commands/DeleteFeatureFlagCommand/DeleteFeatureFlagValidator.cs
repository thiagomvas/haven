using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.FeatureFlags.Commands.DeleteFeatureFlagCommand;

public class DeleteFeatureFlagValidator : AbstractValidator<DeleteFeatureFlagCommand>
{
    public DeleteFeatureFlagValidator()
    {
        RuleFor(x => x.FlagId).ValidId();
    }
}