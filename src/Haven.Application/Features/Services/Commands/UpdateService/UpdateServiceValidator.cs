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

        When(x => x.Type.HasValue, () =>
        {
            RuleFor(x => x.Type.Value)
                .IsInEnum()
                .WithMessage("Service type is invalid.");
        });

        When(x => x.ExposureMode.HasValue, () =>
        {
            RuleFor(x => x.ExposureMode.Value)
                .IsInEnum()
                .WithMessage("Exposure mode is invalid.");
        });

        RuleFor(x => x.Type)
            .Custom((type, context) =>
            {
                var cmd = (UpdateServiceCommand)context.InstanceToValidate;
                if (type.HasValue && type.Value == ServiceType.DockerImage)
                {
                    if (!cmd.DockerConfig.HasValue || cmd.DockerConfig.Value is null)
                    {
                        context.AddFailure("Docker configuration is required for DockerImage service type.");
                    }
                }
            });

        When(x => x.Type.HasValue && x.Type.Value == ServiceType.DockerImage && x.DockerConfig.HasValue && x.DockerConfig.Value is not null, () =>
        {
            RuleFor(x => x.DockerConfig.Value!.Image)
                .NotEmpty()
                .WithMessage("Docker image cannot be empty.");
        });
    }
}
