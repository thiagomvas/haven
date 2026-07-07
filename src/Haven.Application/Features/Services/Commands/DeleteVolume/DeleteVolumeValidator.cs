using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.Services.Commands.DeleteVolume;

public sealed class DeleteVolumeValidator : AbstractValidator<DeleteVolumeCommand>
{
    public DeleteVolumeValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.VolumeId).ValidId();
    }
}
