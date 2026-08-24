using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.ServiceRegistry.Commands.AddDomain;

public sealed class AddDomainValidator : AbstractValidator<AddDomainCommand>
{
    public AddDomainValidator()
    {
        RuleFor(x => x)
            .Must(x => x.ServiceId.HasValue ^ x.SidecarId.HasValue)
            .WithMessage("Exactly one of ServiceId or SidecarId must be provided.");

        RuleFor(x => x.ServiceId.Value).ValidId().When(x => x.ServiceId.HasValue);
        RuleFor(x => x.SidecarId.Value).ValidId().When(x => x.SidecarId.HasValue);

        RuleFor(x => x.Hostname)
            .NotEmpty()
            .WithMessage("Domain hostname cannot be empty.")
            .Must(BeValidHostname)
            .WithMessage("Hostname must be a valid DNS hostname.");

        RuleFor(x => x.ContainerPort)
            .InclusiveBetween(1, 65535)
            .WithMessage("Container port must be between 1 and 65535.");

        RuleFor(x => x.TlsMode).IsInEnum();

        RuleFor(x => x.InternalBasePath)
            .Must(BeValidBasePath)
            .WithMessage("Internal base path must start with '/'.")
            .When(x => !string.IsNullOrWhiteSpace(x.InternalBasePath) && x.InternalBasePath != "/");
    }

    private static bool BeValidHostname(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname)) return false;
        return Uri.CheckHostName(hostname) != UriHostNameType.Unknown;
    }

    private static bool BeValidBasePath(string? path) => path is not null && path.StartsWith('/');
}