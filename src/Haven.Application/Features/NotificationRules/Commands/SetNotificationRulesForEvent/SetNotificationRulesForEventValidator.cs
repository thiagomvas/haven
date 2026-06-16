using FluentValidation;

using Haven.Domain.Events;

namespace Haven.Application.Features.NotificationRules.Commands.SetNotificationRulesForEvent;

public class SetNotificationRulesForEventValidator : AbstractValidator<SetNotificationRulesForEventCommand>
{
    public SetNotificationRulesForEventValidator()
    {
        RuleFor(x => x.EventType)
            .NotEmpty()
            .WithMessage("Event type cannot be empty.")
            .Must(e => DomainEvent.AllEventTypes.Any(t => t.Name == e))
            .WithMessage("Event type is not a valid domain event.");

        RuleFor(x => x.ChannelIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Channel IDs must not contain duplicates.")
            .When(x => x.ChannelIds.Count > 0);
    }
}