using FluentValidation;

namespace Haven.Application.Features.Services.Commands.ApplyManifestForService;

public sealed class ApplyManifestForServiceValidator : AbstractValidator<ApplyManifestForServiceCommand>
{
    public ApplyManifestForServiceValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Project ID cannot be empty.");
        RuleFor(x => x.EnvironmentId).NotEmpty().WithMessage("Environment ID cannot be empty.");
        RuleFor(x => x.ServiceId).NotEmpty().WithMessage("Service ID cannot be empty.");
        RuleFor(x => x.ManifestYaml).NotEmpty().WithMessage("Manifest YAML cannot be empty.");
    }
}