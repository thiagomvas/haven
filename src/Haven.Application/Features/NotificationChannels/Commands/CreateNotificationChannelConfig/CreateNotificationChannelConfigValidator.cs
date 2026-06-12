using System.Text.Json;
using FluentValidation;
using Haven.Domain;
using Haven.Domain.Models;

namespace Haven.Application.Features.NotificationChannels.Commands.CreateNotificationChannelConfig;

public class CreateNotificationChannelConfigValidator : AbstractValidator<CreateNotificationChannelConfigCommand>
{
    private static readonly WebhookNotificationConfigValidator WebhookValidator = new();

    public CreateNotificationChannelConfigValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name cannot be empty.");

        RuleFor(x => x.Channel)
            .IsInEnum()
            .WithMessage("Channel must be a valid notification channel.");

        RuleFor(x => x.ConfigJson)
            .NotEmpty()
            .WithMessage("Config cannot be empty.")
            .DependentRules(() =>
            {
                When(x => x.Channel == NotificationChannel.Webhook, () =>
                {
                    RuleFor(x => x.ConfigJson)
                        .Custom((json, ctx) =>
                        {
                            WebhookNotificationConfig? config;
                            try
                            {
                                config = JsonSerializer.Deserialize<WebhookNotificationConfig>(json,
                                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            }
                            catch
                            {
                                ctx.AddFailure("ConfigJson", "Webhook config is not valid JSON.");
                                return;
                            }

                            if (config is null)
                            {
                                ctx.AddFailure("ConfigJson", "Webhook config cannot be null.");
                                return;
                            }

                            var result = WebhookValidator.Validate(config);
                            foreach (var failure in result.Errors)
                                ctx.AddFailure("ConfigJson." + failure.PropertyName, failure.ErrorMessage);
                        });
                });
            });
    }
}
