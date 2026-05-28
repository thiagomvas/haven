using FluentValidation;

namespace Haven.Application.Features.Business.Queries.FuzzySearch;

public class FuzzySearchValidator : AbstractValidator<FuzzySearchQuery>
{
    public FuzzySearchValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .WithMessage("Search query cannot be empty.")
            .MaximumLength(100)
            .WithMessage("Search query cannot exceed 100 characters.");

        RuleFor(x => x.Count)
            .GreaterThan(0)
            .WithMessage("Count must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("Count cannot exceed 100.");
    }
}