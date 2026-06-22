using FluentValidation;

namespace Haven.Application.Features.Configuration.Commands.ApplyConfigurationManifest;

public sealed class ApplyConfigurationManifestValidator : AbstractValidator<ApplyConfigurationManifestCommand>
{
    public ApplyConfigurationManifestValidator()
    {
        RuleFor(x => x.ManifestYaml).NotEmpty().WithMessage("Manifest YAML cannot be empty.");
    }
}
