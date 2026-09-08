using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.Services.Commands.BulkRestartServices;

public sealed class BulkRestartServicesValidator : AbstractValidator<BulkRestartServicesCommand>
{
    public BulkRestartServicesValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Project ID cannot be empty.");
        RuleFor(x => x.EnvironmentId).NotEmpty().WithMessage("Environment ID cannot be empty.");

        RuleFor(x => x.ServiceIds)
            .NotEmpty()
            .WithMessage("At least one service ID is required.");

        RuleForEach(x => x.ServiceIds).ValidId();
    }
}