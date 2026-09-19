using FluentValidation;

namespace Haven.Application.Features.HealthChecks;

public static class HealthCheckRuleExtensions
{
    public const int MaxRetries = 10;
    public const int MaxThreshold = 100;

    public static IRuleBuilderOptions<T, int> HealthCheckRetries<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(0, MaxRetries)
            .WithMessage($"Retries must be between 0 and {MaxRetries}.");

    public static IRuleBuilderOptions<T, int> HealthCheckThreshold<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, MaxThreshold)
            .WithMessage($"Thresholds must be between 1 and {MaxThreshold}.");
}
