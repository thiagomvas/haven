using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.Secrets.Commands.CreateSecretVariable;

public sealed class CreateSecretVariableValidator : AbstractValidator<CreateSecretVariableCommand>
{
    public CreateSecretVariableValidator()
    {
        RuleFor(x => x.ParentId).ValidId();

        RuleFor(x => x.ParentType)
            .IsInEnum()
            .WithMessage("Parent type must be a valid value.");

        RuleFor(x => x.Key)
            .NotEmpty()
            .WithMessage("Key cannot be empty.")
            .MaximumLength(128)
            .WithMessage("Key cannot exceed 128 characters.");

        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Value cannot be empty.");
    }
}
