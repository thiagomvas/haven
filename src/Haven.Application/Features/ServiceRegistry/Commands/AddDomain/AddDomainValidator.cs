using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.ServiceRegistry.Commands.AddDomain;

public sealed class AddDomainValidator : AbstractValidator<AddDomainCommand>
{
    public AddDomainValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();

        RuleFor(x => x.Hostname)
            .NotEmpty()
            .WithMessage("Domain hostname cannot be empty.")
            .Must(BeValidHostname)
            .WithMessage("Hostname must be a valid DNS hostname.");

        RuleFor(x => x.ContainerPort)
            .InclusiveBetween(1, 65535)
            .WithMessage("Container port must be between 1 and 65535.");
    }

    private static bool BeValidHostname(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname)) return false;
        return Uri.CheckHostName(hostname) != UriHostNameType.Unknown;
    }
}