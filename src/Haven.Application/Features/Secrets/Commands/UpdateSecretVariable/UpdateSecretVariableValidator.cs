using FluentValidation;

namespace Haven.Application.Features.Secrets.Commands.UpdateSecretVariable;

public sealed class UpdateSecretVariableValidator : AbstractValidator<UpdateSecretVariableCommand>
{
    public UpdateSecretVariableValidator()
    {
        RuleFor(x => x.Key.Value)
            .NotEmpty()
            .WithMessage("Key cannot be empty.")
            .MaximumLength(128)
            .WithMessage("Key cannot exceed 128 characters.")
            .When(x => x.Key.HasValue);

        RuleFor(x => x.Value.Value)
            .NotEmpty()
            .WithMessage("Value cannot be empty.")
            .When(x => x.Value.HasValue);
    }
}
