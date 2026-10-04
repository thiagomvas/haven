using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.CustomActions.Commands.UpdateCustomAction;

public sealed class UpdateCustomActionValidator : AbstractValidator<UpdateCustomActionCommand>
{
    public UpdateCustomActionValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.ActionId).ValidId();

        RuleFor(x => x.ActionName).NotEmptyWhenProvided();
        RuleFor(x => x.Alias).NotEmptyWhenProvided();
        RuleFor(x => x.Icon).NotEmptyWhenProvided();

        RuleFor(x => x.Config!)
            .SetValidator(new ActionConfigValidator())
            .When(x => x.Config is not null);

        RuleFor(x => x.Risk)
            .IsInEnum()
            .When(x => x.Risk.HasValue)
            .WithMessage("Risk must be a valid action risk.");

        RuleFor(x => x.Inputs!)
            .SetValidator(new CustomActionInputsValidator())
            .When(x => x.Inputs is not null);

        RuleFor(x => x.Timeout)
            .GreaterThan(TimeSpan.Zero)
            .When(x => x.Timeout.HasValue)
            .WithMessage("Timeout must be greater than zero.");
    }
}
