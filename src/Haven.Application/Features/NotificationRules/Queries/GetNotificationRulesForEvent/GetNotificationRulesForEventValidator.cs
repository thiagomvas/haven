using FluentValidation;
using Haven.Domain.Events;

namespace Haven.Application.Features.NotificationRules.Queries.GetNotificationRulesForEvent;

public class GetNotificationRulesForEventValidator : AbstractValidator<GetNotificationRulesForEventQuery>
{
    public GetNotificationRulesForEventValidator()
    {
        RuleFor(x => x.EventType)
            .NotEmpty()
            .WithMessage("Event type cannot be empty.")
            .Must(e => DomainEvent.AllEventTypes.Any(t => t.Name == e))
            .WithMessage("Event type is not a valid domain event.");
    }
}
