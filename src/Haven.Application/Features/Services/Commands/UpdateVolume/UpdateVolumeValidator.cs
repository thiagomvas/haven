using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.Services.Commands.UpdateVolume;

public sealed class UpdateVolumeValidator : AbstractValidator<UpdateVolumeCommand>
{
    public UpdateVolumeValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.VolumeId).ValidId();

        RuleFor(x => x.Name)
            .NotEmpty()
            .When(x => x.Name is not null)
            .WithMessage("Volume name cannot be empty.");

        RuleFor(x => x.Target)
            .Must(t => t!.StartsWith('/'))
            .When(x => x.Target is not null)
            .WithMessage("Volume target must be an absolute container path (starting with '/').");
    }
}
