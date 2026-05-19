using FluentValidation;

namespace Haven.Application.Features.Git.Queries.GetRemoteBranches;

public sealed class GetRemoteBranchesValidator : AbstractValidator<GetRemoteBranchesQuery>
{
    public GetRemoteBranchesValidator()
    {
        RuleFor(x => x.RepositoryUrl)
            .NotEmpty()
            .WithMessage("Repository URL cannot be empty.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Repository URL must be a valid absolute URI.");
    }
}
