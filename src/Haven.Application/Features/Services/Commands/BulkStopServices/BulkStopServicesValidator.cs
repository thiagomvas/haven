using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.Services.Commands.BulkStopServices;

public sealed class BulkStopServicesValidator : AbstractValidator<BulkStopServicesCommand>
{
    public BulkStopServicesValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Project ID cannot be empty.");
        RuleFor(x => x.EnvironmentId).NotEmpty().WithMessage("Environment ID cannot be empty.");

        RuleFor(x => x.ServiceIds)
            .NotEmpty()
            .WithMessage("At least one service ID is required.");

        RuleForEach(x => x.ServiceIds).ValidId();
    }
}