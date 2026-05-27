using FluentValidation;

namespace Haven.Application.Features.Projects.Queries.GetProjectDashboard;

public sealed class GetProjectDashboardValidator : AbstractValidator<GetProjectDashboardQuery>
{
    public GetProjectDashboardValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project ID is required.");
    }
}
