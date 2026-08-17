using FluentValidation;

namespace Haven.Application.Features.Sidecars.Commands.EnableSidecar;

public class EnableSidecarValidator : AbstractValidator<EnableSidecarCommand>
{
    public EnableSidecarValidator()
    {
        RuleFor(x => x.SidecarId)
            .NotEmpty()
            .WithMessage("Sidecar ID cannot be empty.");
    }
}