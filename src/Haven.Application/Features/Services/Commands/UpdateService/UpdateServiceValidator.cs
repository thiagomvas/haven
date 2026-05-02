using FluentValidation;
using Haven.Domain;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.Services.Commands.UpdateService;

public sealed class UpdateServiceValidator : AbstractValidator<UpdateServiceCommand>
{
    private static readonly HashSet<string> ReservedNames =
        new(StringComparer.OrdinalIgnoreCase) { "haven", "dns", "localhost", "host", "internal" };

    public UpdateServiceValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project ID cannot be empty.");

        RuleFor(x => x.EnvironmentId)
            .NotEmpty()
            .WithMessage("Environment ID cannot be empty.");

        RuleFor(x => x.ServiceId)
            .NotEmpty()
            .WithMessage("Service ID cannot be empty.");

        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n.Value))
            .When(x => x.Name.HasValue)
            .WithMessage("Service name cannot be empty.");

        When(x => x.Name.HasValue, () =>
        {
            RuleFor(x => x.Name.Value)
                .Matches(HavenServiceName.ValidPattern)
                .WithMessage("Service name may only contain letters, digits, spaces, hyphens, and underscores.")
                .Must(name => !ReservedNames.Contains(name))
                .WithMessage("Service name is reserved and cannot be used.");
        });

        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue)
            .WithMessage("Service type is invalid.");

        RuleFor(x => x.ExposureMode)
            .IsInEnum()
            .When(x => x.ExposureMode.HasValue)
            .WithMessage("Exposure mode is invalid.");

        When(x => x.Type.HasValue && x.Type.Value == ServiceType.DockerImage, () =>
        {
            RuleFor(x => x.DockerConfig)
                .NotNull()
                .WithMessage("Docker configuration is required for DockerImage service type.");

            RuleFor(x => x.DockerConfig!.Value!.Image)
                .NotEmpty()
                .When(x => x.DockerConfig!.HasValue && x.DockerConfig.Value is not null)
                .WithMessage("Docker image cannot be empty.");
        });
    }
}
