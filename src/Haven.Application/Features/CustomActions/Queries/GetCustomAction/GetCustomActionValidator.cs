using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.CustomActions.Queries.GetCustomAction;

public sealed class GetCustomActionValidator : AbstractValidator<GetCustomActionQuery>
{
    public GetCustomActionValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.ActionId).ValidId();
    }
}
