using FluentValidation;

using Haven.Application.Extensions;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.FeatureFlags.Commands.UpdateFeatureFlagCommand;

public class UpdateFeatureFlagValidator : AbstractValidator<UpdateFeatureFlagCommand>
{
    public UpdateFeatureFlagValidator()
    {
        RuleFor(x => x.FlagId).ValidId();
        RuleFor(x => x.Name)
            .NotEmpty()
            .When(x => x.Name is not null)
            .WithMessage("Name cannot be empty when provided.");
        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type is not null)
            .WithMessage("Type must be a valid feature flag type.");
        RuleFor(x => x.Key)
            .NotEmpty()
            .When(x => x.Type == FeatureFlagType.EnvironmentVariable)
            .WithMessage("Key is required when Type is EnvironmentVariable.");
        RuleFor(x => x.ValueType)
            .IsInEnum()
            .When(x => x.ValueType is not null)
            .WithMessage("ValueType must be a valid feature flag value type.");
    }
}