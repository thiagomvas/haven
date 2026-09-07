using FluentValidation;

using Haven.Application.Configuration;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Configuration.Commands.UpdateTraefikDashboardAuth;

public sealed class UpdateTraefikDashboardAuthValidator : AbstractValidator<UpdateTraefikDashboardAuthCommand>
{
    public UpdateTraefikDashboardAuthValidator(IOptionsMonitor<TraefikOptions> options)
    {
        When(x => x.Enabled, () =>
        {
            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage("Username is required when dashboard auth is enabled.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required the first time dashboard auth is enabled.")
                .When(_ => options.CurrentValue.DashboardAuthPasswordHash is null);
        });
    }
}