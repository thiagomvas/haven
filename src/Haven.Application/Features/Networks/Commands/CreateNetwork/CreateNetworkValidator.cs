using FluentValidation;

namespace Haven.Application.Features.Networks.Commands.CreateNetwork;

public sealed class CreateNetworkValidator : AbstractValidator<CreateNetworkCommand>
{
    public CreateNetworkValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Network name is required")
            .MaximumLength(255)
            .WithMessage("Network name must not exceed 255 characters");
    }
}