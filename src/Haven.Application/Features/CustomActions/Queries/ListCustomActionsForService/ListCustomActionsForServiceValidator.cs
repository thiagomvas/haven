using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.CustomActions.Queries.ListCustomActionsForService;

public sealed class ListCustomActionsForServiceValidator : AbstractValidator<ListCustomActionsForServiceQuery>
{
    public ListCustomActionsForServiceValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
    }
}
