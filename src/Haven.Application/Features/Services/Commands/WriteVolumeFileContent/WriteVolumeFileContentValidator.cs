using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.Services.Commands.WriteVolumeFileContent;

public sealed class WriteVolumeFileContentValidator : AbstractValidator<WriteVolumeFileContentCommand>
{
    public WriteVolumeFileContentValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.VolumeId).ValidId();
        RuleFor(x => x.Path)
            .NotEmpty()
            .WithMessage("File path cannot be empty.");
    }
}
