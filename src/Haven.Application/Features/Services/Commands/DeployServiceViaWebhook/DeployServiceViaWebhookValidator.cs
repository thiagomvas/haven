using FluentValidation;

namespace Haven.Application.Features.Services.Commands.DeployServiceViaWebhook;

public sealed class DeployServiceViaWebhookValidator : AbstractValidator<DeployServiceViaWebhookCommand>
{
    public DeployServiceViaWebhookValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Token cannot be empty.");
    }
}
