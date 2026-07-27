using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.HealthChecks.Commands.UpdateHealthCheckCommand;

public class UpdateHealthCheckValidator : AbstractValidator<UpdateHealthCheckCommand>
{
    public UpdateHealthCheckValidator()
    {
        RuleFor(x => x.HealthCheckId).ValidId();
        RuleFor(x => x.Name)
            .NotEmpty()
            .When(x => x.Name is not null)
            .WithMessage("Name cannot be empty when provided.");
        RuleFor(x => x.CronExpression)
            .Must(HealthCheckCronValidator.IsValid)
            .When(x => x.CronExpression is not null && !x.ClearCronExpression)
            .WithMessage("Cron expression is invalid.");
        RuleFor(x => x)
            .Must(x => !(x.CronExpression is not null && x.ClearCronExpression))
            .WithMessage("Cannot both set and clear the cron expression.");
    }
}
