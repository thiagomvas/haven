using System.Net;

using FluentValidation;

namespace Haven.Application.Features.Sidecars.Commands.UpdateSidecar;

public sealed class UpdateSidecarValidator : AbstractValidator<UpdateSidecarCommand>
{
    public UpdateSidecarValidator()
    {
        RuleFor(x => x.SidecarId)
            .NotEmpty()
            .WithMessage("Sidecar ID cannot be empty.");

        When(x => x.DockerConfig.HasValue && x.DockerConfig.Value is not null, () =>
        {
            RuleFor(x => x.DockerConfig.Value!.Image)
                .NotEmpty()
                .WithMessage("Docker image cannot be empty.");

            RuleForEach(x => x.DockerConfig.Value!.Ports)
                .Must(BeAValidPortMapping)
                .WithMessage("Port mapping must be in the format 'hostPort:containerPort' or 'hostIp:hostPort:containerPort'.");

            RuleForEach(x => x.DockerConfig.Value!.CommandArgs)
                .Must(a => !string.IsNullOrWhiteSpace(a))
                .WithMessage("Command argument cannot be empty.");
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