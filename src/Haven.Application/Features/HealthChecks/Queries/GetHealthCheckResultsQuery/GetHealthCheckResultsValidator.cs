using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.HealthChecks.Queries.GetHealthCheckResultsQuery;

public class GetHealthCheckResultsValidator : AbstractValidator<GetHealthCheckResultsQuery>
{
    public GetHealthCheckResultsValidator()
    {
        RuleFor(x => x.HealthCheckId).ValidId();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 200)
            .WithMessage("Limit must be between 1 and 200.");
    }
}
