using FluentValidation;

namespace Haven.Application.Features.Environments.Queries.ExportEnvironmentManifest;

public sealed class ExportEnvironmentManifestValidator : AbstractValidator<ExportEnvironmentManifestQuery>
{
    public ExportEnvironmentManifestValidator()
    {
        RuleFor(x => x.EnvironmentId)
            .NotEmpty()
            .WithMessage("Environment ID cannot be empty.");
    }
}
