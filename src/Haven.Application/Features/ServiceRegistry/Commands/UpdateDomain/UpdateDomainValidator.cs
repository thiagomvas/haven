using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.ServiceRegistry.Commands.UpdateDomain;

public sealed class UpdateDomainValidator : AbstractValidator<UpdateDomainCommand>
{
    public UpdateDomainValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.DomainId).ValidId();

        RuleFor(x => x.Hostname)
            .NotEmpty()
            .Must(BeValidHostname)
            .WithMessage("Hostname must be a valid DNS hostname.")
            .When(x => x.Hostname is not null);

        RuleFor(x => x.ContainerPort)
            .InclusiveBetween(1, 65535)
            .WithMessage("Container port must be between 1 and 65535.")
            .When(x => x.ContainerPort.HasValue);
    }

    private static bool BeValidHostname(string? hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname)) return false;
        return Uri.CheckHostName(hostname) != UriHostNameType.Unknown;
    }
}