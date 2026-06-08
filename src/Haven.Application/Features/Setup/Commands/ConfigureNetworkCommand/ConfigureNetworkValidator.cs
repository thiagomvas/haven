using FluentValidation;

namespace Haven.Application.Features.Setup.Commands.ConfigureNetworkCommand;

public class ConfigureNetworkValidator : AbstractValidator<ConfigureNetworkCommand>
{
    public ConfigureNetworkValidator()
    {
        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535).WithMessage("Port must be between 1 and 65535.")
            .When(x => x.Port.HasValue);
    }
}
