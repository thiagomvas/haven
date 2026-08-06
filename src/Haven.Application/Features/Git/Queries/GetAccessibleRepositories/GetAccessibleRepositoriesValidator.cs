using FluentValidation;

namespace Haven.Application.Features.Git.Queries.GetAccessibleRepositories;

public sealed class GetAccessibleRepositoriesValidator : AbstractValidator<GetAccessibleRepositoriesQuery>
{
    public GetAccessibleRepositoriesValidator()
    {
        RuleFor(x => x.GitCredentialId)
            .NotEmpty()
            .WithMessage("Git credential is required.");
    }
}