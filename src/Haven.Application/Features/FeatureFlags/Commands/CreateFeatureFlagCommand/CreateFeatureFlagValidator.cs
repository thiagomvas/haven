using FluentValidation;
using Haven.Application.Extensions;
using Haven.Domain;

namespace Haven.Application.Features.FeatureFlags.Commands.CreateFeatureFlagCommand;

public class CreateFeatureFlagValidator : AbstractValidator<CreateFeatureFlagCommand>
{
    public CreateFeatureFlagValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Feature flag name cannot be empty.");
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type must be a valid feature flag type.");
        RuleFor(x => x.Key)
            .NotEmpty()
            .When(x => x.Type == FeatureFlagType.EnvironmentVariable)
            .WithMessage("Key is required when Type is EnvironmentVariable.");
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Feature flag value cannot be empty.");
        RuleFor(x => x.ValueType)
            .IsInEnum()
            .WithMessage("ValueType must be a valid feature flag value type.");
    }
}
