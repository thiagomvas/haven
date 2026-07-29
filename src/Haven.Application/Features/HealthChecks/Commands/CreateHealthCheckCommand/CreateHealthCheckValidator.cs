using FluentValidation;

using Haven.Application.Extensions;
using Haven.Domain;

namespace Haven.Application.Features.HealthChecks.Commands.CreateHealthCheckCommand;

public class CreateHealthCheckValidator : AbstractValidator<CreateHealthCheckCommand>
{
    public CreateHealthCheckValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Health check name cannot be empty.");
        RuleFor(x => x.Kind)
            .IsInEnum()
            .WithMessage("Kind must be a valid health check kind.");
        RuleFor(x => x.CronExpression)
            .Must(HealthCheckCronValidator.IsValid)
            .When(x => x.CronExpression is not null)
            .WithMessage("Cron expression is invalid.");
        RuleFor(x => x.Config)
            .Must((command, config) => HealthCheckConfigValidator.IsValid(command.Kind, config))
            .WithMessage("Config is not valid for the selected health check kind.");
    }
}