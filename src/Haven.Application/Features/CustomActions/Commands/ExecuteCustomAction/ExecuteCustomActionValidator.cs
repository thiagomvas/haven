using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.CustomActions.Commands.ExecuteCustomAction;

public sealed class ExecuteCustomActionValidator : AbstractValidator<ExecuteCustomActionCommand>
{
    public ExecuteCustomActionValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.ActionId).ValidId();
    }
}