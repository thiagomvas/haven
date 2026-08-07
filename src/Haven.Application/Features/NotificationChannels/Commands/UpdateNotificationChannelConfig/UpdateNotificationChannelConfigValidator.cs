using FluentValidation;

namespace Haven.Application.Features.NotificationChannels.Commands.UpdateNotificationChannelConfig;

public class UpdateNotificationChannelConfigValidator : AbstractValidator<UpdateNotificationChannelConfigCommand>
{
    public UpdateNotificationChannelConfigValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id cannot be empty.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name cannot be empty.");

        RuleFor(x => x.ConfigJson)
            .NotEmpty()
            .WithMessage("Config cannot be empty.");
    }
}