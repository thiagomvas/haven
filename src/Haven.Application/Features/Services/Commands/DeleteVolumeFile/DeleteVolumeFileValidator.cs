using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.Services.Commands.DeleteVolumeFile;

public sealed class DeleteVolumeFileValidator : AbstractValidator<DeleteVolumeFileCommand>
{
    public DeleteVolumeFileValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.VolumeId).ValidId();
        RuleFor(x => x.Path)
            .NotEmpty()
            .WithMessage("File path cannot be empty.");
    }
}