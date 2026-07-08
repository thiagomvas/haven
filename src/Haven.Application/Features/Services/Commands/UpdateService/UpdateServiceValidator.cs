using System.Net;

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
                        context.AddFailure("Docker configuration is required for DockerImage service type.");
                }
                if (type.HasValue && type.Value == ServiceType.Dockerfile)
                {
                    if (!cmd.DockerfileConfig.HasValue || cmd.DockerfileConfig.Value is null)
                        context.AddFailure("Dockerfile configuration is required for Dockerfile service type.");
                }
            });

        When(x => x.Type.HasValue && x.Type.Value == ServiceType.DockerImage && x.DockerConfig.HasValue && x.DockerConfig.Value is not null, () =>
        {
            RuleFor(x => x.DockerConfig.Value!.Image)
                .NotEmpty()
                .WithMessage("Docker image cannot be empty.");
        });

        When(x => x.DockerConfig.HasValue && x.DockerConfig.Value is not null && x.ExposureMode.HasValue, () =>
        {
            When(x => x.ExposureMode.Value == ExposureMode.Custom, () =>
            {
                RuleForEach(x => x.DockerConfig.Value!.Ports)
                    .Must(BeAValidPortMapping)
                    .WithMessage("Port mapping must be in the format 'hostPort:containerPort' or 'hostIp:hostPort:containerPort'.");
            });

            When(x => x.ExposureMode.Value != ExposureMode.Custom, () =>
            {
                RuleForEach(x => x.DockerConfig.Value!.Ports)
                    .Must(p => p.Split(':').Length <= 2)
                    .WithMessage("Host IP in port mappings is only allowed when Exposure Mode is Custom.");
            });
        });

        When(x => x.DockerfileConfig.HasValue && x.DockerfileConfig.Value is not null && x.DockerfileConfig.Value.Source == DockerfileSource.Git, () =>
        {
            RuleFor(x => x.DockerfileConfig.Value!.Repository)
                .NotEmpty()
                .WithMessage("Repository URL is required for Git-sourced Dockerfile.");

            RuleFor(x => x.DockerfileConfig.Value!.Branch)
                .NotEmpty()
                .WithMessage("Branch is required for Git-sourced Dockerfile.");
        });

        When(x => x.DockerfileConfig.HasValue && x.DockerfileConfig.Value is not null && x.DockerfileConfig.Value.Source == DockerfileSource.Raw, () =>
        {
            RuleFor(x => x.DockerfileConfig.Value!.Content)
                .NotEmpty()
                .WithMessage("Dockerfile content is required for raw Dockerfile.");
        });
    }

    private static bool BeAValidPortMapping(string mapping)
    {
        if (string.IsNullOrWhiteSpace(mapping))
            return false;

        var parts = mapping.Split(':');
        return parts.Length switch
        {
            2 => IsValidHostPort(parts[0]) && IsValidContainerPort(parts[1]),
            3 => IPAddress.TryParse(parts[0], out _) && IsValidHostPort(parts[1]) && IsValidContainerPort(parts[2]),
            _ => false
        };
    }

    private static bool IsValidHostPort(string segment) =>
        int.TryParse(segment, out var port) && port is > 0 and <= 65535;

    private static bool IsValidContainerPort(string segment)
    {
        var portPart = segment.Split('/')[0];
        return int.TryParse(portPart, out var port) && port is > 0 and <= 65535;
    }
}