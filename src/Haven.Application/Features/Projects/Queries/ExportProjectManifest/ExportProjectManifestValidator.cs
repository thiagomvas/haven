using FluentValidation;

namespace Haven.Application.Features.Projects.Queries.ExportProjectManifest;

public sealed class ExportProjectManifestValidator : AbstractValidator<ExportProjectManifestQuery>
{
    public ExportProjectManifestValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project ID cannot be empty.");

        RuleFor(x => x.IncludeEnvironments)
            .Equal(true)
            .When(x => x.IncludeServices)
            .WithMessage("Services can only be included when environments are included.");
    }
}
