using FluentValidation;

namespace Haven.Application.Features.Setup.Commands.ConfigureNetworkCommand;

public class ConfigureNetworkValidator : AbstractValidator<ConfigureNetworkCommand>
{
    public ConfigureNetworkValidator()
    {
        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535).WithMessage("Port must be between 1 and 65535.")
            .When(x => x.Port.HasValue);

        RuleFor(x => x.Domain)
            .Must(BeValidDomain).WithMessage("Domain must be a valid hostname or IP address.")
            .When(x => x.Domain is { Length: > 0 });
    }

    private static bool BeValidDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return true;
        return Uri.CheckHostName(domain) != UriHostNameType.Unknown;
    }
}
