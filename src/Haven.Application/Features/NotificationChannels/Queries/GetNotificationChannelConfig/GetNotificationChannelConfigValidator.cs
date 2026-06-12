using FluentValidation;
using Haven.Application.Extensions;

namespace Haven.Application.Features.NotificationChannels.Queries.GetNotificationChannelConfig;

public class GetNotificationChannelConfigValidator : AbstractValidator<GetNotificationChannelConfigQuery>
{
    public GetNotificationChannelConfigValidator()
    {
        RuleFor(x => x.Id).ValidId();
    }
}
