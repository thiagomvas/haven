using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.CustomActions.Commands.CreateCustomAction;

public sealed class CreateCustomActionValidator : AbstractValidator<CreateCustomActionCommand>
{
    public CreateCustomActionValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();

        RuleFor(x => x.ActionName)
            .NotEmpty()
            .WithMessage("Action name cannot be empty.");

        RuleFor(x => x.Alias)
            .NotEmpty()
            .WithMessage("Action alias cannot be empty.");

        RuleFor(x => x.ActionDescription)
            .NotNull()
            .WithMessage("Action description cannot be null.");

        RuleFor(x => x.Icon)
            .NotEmpty()
            .WithMessage("Action icon cannot be empty.");

        RuleFor(x => x.Config)
            .NotNull()
            .WithMessage("Action config is required.")
            .SetValidator(new ActionConfigValidator());

        RuleFor(x => x.RequiredPermissions)
            .NotNull()
            .WithMessage("Required permissions cannot be null.");

        RuleFor(x => x.Risk)
            .IsInEnum()
            .WithMessage("Risk must be a valid action risk.");

        RuleFor(x => x.Inputs)
            .NotNull()
            .WithMessage("Inputs cannot be null.")
            .SetValidator(new CustomActionInputsValidator());

        RuleFor(x => x.Timeout)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("Timeout must be greater than zero.");
    }
}
