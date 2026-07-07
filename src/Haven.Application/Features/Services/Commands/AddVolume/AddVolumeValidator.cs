using FluentValidation;

using Haven.Application.Extensions;
using Haven.Domain;

namespace Haven.Application.Features.Services.Commands.AddVolume;

public sealed class AddVolumeValidator : AbstractValidator<AddVolumeCommand>
{
    public AddVolumeValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type must be a valid volume type.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Volume name cannot be empty.");

        RuleFor(x => x.Target)
            .NotEmpty()
            .WithMessage("Volume target cannot be empty.")
            .Must(t => t.StartsWith('/'))
            .WithMessage("Volume target must be an absolute container path (starting with '/').");

        RuleFor(x => x.Source)
            .NotEmpty()
            .When(x => x.Type is VolumeType.HostPath or VolumeType.Named)
            .WithMessage("A source is required for named and host-path volumes.");

        RuleFor(x => x.Source)
            .Must(s => s!.StartsWith('/'))
            .When(x => x.Type == VolumeType.HostPath && !string.IsNullOrEmpty(x.Source))
            .WithMessage("Host-path volume source must be an absolute host path (starting with '/').");
    }
}
