using FluentValidation;

namespace Haven.Application.Features.Sidecars.Commands.DisableSidecar;

public class DisableSidecarValidator : AbstractValidator<DisableSidecarCommand>
{
    public DisableSidecarValidator()
    {
        RuleFor(x => x.SidecarId)
            .NotEmpty()
            .WithMessage("Sidecar ID cannot be empty.");
    }
}