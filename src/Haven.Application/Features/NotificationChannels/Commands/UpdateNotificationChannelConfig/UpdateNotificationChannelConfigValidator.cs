using System.Text.Json;

using FluentValidation;

using Haven.Application.Features.NotificationChannels.Commands.CreateNotificationChannelConfig;
using Haven.Domain.Models;

namespace Haven.Application.Features.NotificationChannels.Commands.UpdateNotificationChannelConfig;

public class UpdateNotificationChannelConfigValidator : AbstractValidator<UpdateNotificationChannelConfigCommand>
{
    private static readonly WebhookNotificationConfigValidator WebhookValidator = new();

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
            .WithMessage("Config cannot be empty.")
            .DependentRules(() =>
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
                            ctx.AddFailure("ConfigJson", "Config is not valid JSON.");
                            return;
                        }

                        if (config is null)
                        {
                            ctx.AddFailure("ConfigJson", "Config cannot be null.");
                            return;
                        }

                        var result = WebhookValidator.Validate(config);
                        foreach (var failure in result.Errors)
                            ctx.AddFailure("ConfigJson." + failure.PropertyName, failure.ErrorMessage);
                    });
            });
    }
}