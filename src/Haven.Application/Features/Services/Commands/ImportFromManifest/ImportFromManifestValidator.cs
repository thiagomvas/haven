using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.Services.Commands.ImportFromManifest;

public class ImportFromManifestValidator : AbstractValidator<ImportFromManifestCommand>
{
    public ImportFromManifestValidator()
    {
        RuleFor(x => x.RawManifest)
            .NotEmptyWhenProvided()
            .WithMessage("Raw manifest cannot be empty when provided.");
    }
}