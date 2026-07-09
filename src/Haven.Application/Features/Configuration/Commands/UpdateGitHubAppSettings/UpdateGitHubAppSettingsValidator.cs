using FluentValidation;

namespace Haven.Application.Features.Configuration.Commands.UpdateGitHubAppSettings;

public sealed class UpdateGitHubAppSettingsValidator : AbstractValidator<UpdateGitHubAppSettingsCommand>
{
    public UpdateGitHubAppSettingsValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("Client ID is required.");

        RuleFor(x => x.ClientSecret.Value)
            .NotEmpty().WithMessage("Client secret cannot be blank.")
            .When(x => x.ClientSecret.HasValue);
    }
}