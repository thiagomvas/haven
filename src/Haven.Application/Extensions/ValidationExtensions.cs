using FluentValidation;

namespace Haven.Application.Extensions;

public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, Guid> ValidId<T>(this IRuleBuilder<T, Guid> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .NotNull()
            .NotEqual(Guid.Empty);
    }

    public static IRuleBuilderOptions<T, string?> NotEmptyWhenProvided<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => value is null || !string.IsNullOrWhiteSpace(value))
            .WithMessage("{PropertyName} cannot be empty when provided.");
    }
}