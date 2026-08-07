using FluentValidation;

namespace Haven.Application.Features.NotificationChannels.Commands.SetSystemDefaultNotificationChannel;

public class SetSystemDefaultNotificationChannelValidator : AbstractValidator<SetSystemDefaultNotificationChannelCommand>
{
    public SetSystemDefaultNotificationChannelValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}