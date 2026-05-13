using FluentValidation;
using Haven.Application.Extensions;

namespace Haven.Application.Features.FeatureFlags.Commands.UpdateFeatureFlagCommand;

public class UpdateFeatureFlagValidator : AbstractValidator<UpdateFeatureFlagCommand>
{
    public UpdateFeatureFlagValidator()
    {
        RuleFor(x => x.FlagId).ValidId();
        RuleFor(x => x.Name).NotEmptyWhenProvided();
        RuleFor(x => x.Description).NotEmptyWhenProvided();
        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type is not null)
            .WithMessage("Type must be a valid feature flag type.");
        RuleFor(x => x.Value).NotEmptyWhenProvided();
        RuleFor(x => x.ValueType)
            .IsInEnum()
            .When(x => x.ValueType is not null)
            .WithMessage("ValueType must be a valid feature flag value type.");
    }
}