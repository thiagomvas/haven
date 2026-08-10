using FluentValidation;

using Haven.Application.Features.HealthChecks;

namespace Haven.Application.Features.RepositoryCleanup.Commands.UpdateRepositoryCleanupOptions;

public sealed class UpdateRepositoryCleanupOptionsValidator : AbstractValidator<UpdateRepositoryCleanupOptionsCommand>
{
    public UpdateRepositoryCleanupOptionsValidator()
    {
        RuleFor(x => x.Options.CronExpression)
            .Must(HealthCheckCronValidator.IsValid)
            .WithMessage("Cron expression is invalid.");

        RuleFor(x => x.Options.GracePeriodHours)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Grace period must be zero or a positive number of hours.");
    }
}
