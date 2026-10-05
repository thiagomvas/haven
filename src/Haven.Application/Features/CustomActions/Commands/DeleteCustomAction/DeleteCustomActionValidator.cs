using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.CustomActions.Commands.DeleteCustomAction;

public sealed class DeleteCustomActionValidator : AbstractValidator<DeleteCustomActionCommand>
{
    public DeleteCustomActionValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.ActionId).ValidId();
    }
}