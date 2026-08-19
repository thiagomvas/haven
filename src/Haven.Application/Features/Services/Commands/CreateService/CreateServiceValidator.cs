using System.Net;

using FluentValidation;

using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.Services.Commands.CreateService;

public sealed class CreateServiceValidator : AbstractValidator<CreateServiceCommand>
{
    private static readonly HashSet<string> ReservedNames =
        new(StringComparer.OrdinalIgnoreCase) { "haven", "dns", "localhost", "host", "internal" };

    public CreateServiceValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project ID cannot be empty.");

        RuleFor(x => x.EnvironmentId)
            .NotEmpty()
            .WithMessage("Environment ID cannot be empty.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Service name cannot be empty.")
            .Matches(HavenServiceName.ValidPattern)
            .WithMessage("Service name may only contain letters, digits, spaces, hyphens, and underscores.")
            .Must(name => !ReservedNames.Contains(name))
            .WithMessage("Service name is reserved and cannot be used.");

        RuleFor(x => x.Alias)
            .MinimumLength(2)
            .WithMessage("Service alias must be at least 2 characters.")
            .MaximumLength(8)
            .WithMessage("Service alias cannot exceed 8 characters.")
            .Matches(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$")
            .WithMessage("Service alias may only contain lowercase letters, digits, and hyphens, and cannot start or end with a hyphen.")
            .When(x => !string.IsNullOrEmpty(x.Alias));

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Service type is invalid.");

        RuleFor(x => x.ExposureMode)
            .IsInEnum()
            .WithMessage("Exposure mode is invalid.");

        When(x => x.Type == ServiceType.DockerImage, () =>
        {
            RuleFor(x => x.DockerConfig)
                .NotNull()
                .WithMessage("Docker configuration is required for DockerImage service type.");

            RuleFor(x => x.DockerConfig!.Image)
                .NotEmpty()
                .When(x => x.DockerConfig is not null)
                .WithMessage("Docker image cannot be empty.");

            When(x => x.DockerConfig is not null, () =>
            {
                When(x => x.ExposureMode == ExposureMode.Custom, () =>
                {
                    RuleForEach(x => x.DockerConfig!.Ports)
                        .Must(BeAValidPortMapping)
                        .WithMessage("Port mapping must be in the format 'hostPort:containerPort' or 'hostIp:hostPort:containerPort'.");
                });

                When(x => x.ExposureMode != ExposureMode.Custom, () =>
                {
                    RuleForEach(x => x.DockerConfig!.Ports)
                        .Must(p => p.Split(':').Length <= 2)
                        .WithMessage("Host IP in port mappings is only allowed when Exposure Mode is Custom.");
                });
            });
        });

        When(x => x.Type == ServiceType.Dockerfile, () =>
        {
            RuleFor(x => x.DockerfileConfig)
                .NotNull()
                .WithMessage("Dockerfile configuration is required for Dockerfile service type.");

            When(x => x.DockerfileConfig is not null && x.DockerfileConfig.Source == DockerfileSource.Git, () =>
            {
                RuleFor(x => x.DockerfileConfig!.Repository)
                    .NotEmpty()
                    .WithMessage("Repository URL is required for Git-sourced Dockerfile.");

                RuleFor(x => x.DockerfileConfig!.Branch)
                    .NotEmpty()
                    .WithMessage("Branch is required for Git-sourced Dockerfile.");
            });

            When(x => x.DockerfileConfig is not null && x.DockerfileConfig.Source == DockerfileSource.Raw, () =>
            {
                RuleFor(x => x.DockerfileConfig!.Content)
                    .NotEmpty()
                    .WithMessage("Dockerfile content is required for raw Dockerfile.");
            });

            When(x => x.DockerfileConfig is not null, () =>
            {
                When(x => x.ExposureMode == ExposureMode.Custom, () =>
                {
                    RuleForEach(x => x.DockerfileConfig!.Ports)
                        .Must(BeAValidPortMapping)
                        .WithMessage("Port mapping must be in the format 'hostPort:containerPort' or 'hostIp:hostPort:containerPort'.");
                });

                When(x => x.ExposureMode != ExposureMode.Custom, () =>
                {
                    RuleForEach(x => x.DockerfileConfig!.Ports)
                        .Must(p => p.Split(':').Length <= 2)
                        .WithMessage("Host IP in port mappings is only allowed when Exposure Mode is Custom.");
                });
            });
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