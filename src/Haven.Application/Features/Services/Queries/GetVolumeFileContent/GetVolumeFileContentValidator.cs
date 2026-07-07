using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.Services.Queries.GetVolumeFileContent;

public sealed class GetVolumeFileContentValidator : AbstractValidator<GetVolumeFileContentQuery>
{
    public GetVolumeFileContentValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.VolumeId).ValidId();
        RuleFor(x => x.Path)
            .NotEmpty()
            .WithMessage("File path cannot be empty.");
    }
}
