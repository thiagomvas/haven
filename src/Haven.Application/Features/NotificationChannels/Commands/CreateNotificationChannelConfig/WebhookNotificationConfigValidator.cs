using FluentValidation;

using Haven.Application.Common.Contracts.Notifications;

namespace Haven.Application.Features.NotificationChannels.Commands.CreateNotificationChannelConfig;

public class WebhookNotificationConfigValidator : AbstractValidator<WebhookNotificationConfig>
{
    public WebhookNotificationConfigValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .WithMessage("Webhook URL is required.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Webhook URL must be a valid HTTP or HTTPS URL.");

        RuleForEach(x => x.Headers)
            .ChildRules(header =>
            {
                header.RuleFor(h => h.Key)
                    .NotEmpty()
                    .WithMessage("Header name cannot be empty.");
            });
    }
}