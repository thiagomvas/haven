using FluentValidation;

using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.CustomActions;

public sealed class ActionConfigValidator : AbstractValidator<ActionConfig>
{
    public ActionConfigValidator()
    {
        RuleFor(x => x)
            .SetInheritanceValidator(v =>
            {
                v.Add(new ExecActionConfigValidator());
                v.Add(new HttpActionConfigValidator());
            });
    }
}

public sealed class ExecActionConfigValidator : AbstractValidator<ExecActionConfig>
{
    public ExecActionConfigValidator()
    {
        RuleFor(x => x.Command)
            .NotEmpty()
            .WithMessage("Exec action command cannot be empty.");

        RuleFor(x => x.Command)
            .Must(c => !string.IsNullOrWhiteSpace(c[0]))
            .When(x => x.Command is { Count: > 0 })
            .WithMessage("Exec action executable cannot be empty.");

        RuleFor(x => x.Shell)
            .IsInEnum()
            .When(x => x.Shell.HasValue)
            .WithMessage("Shell must be a valid shell type.");
    }
}

public sealed class HttpActionConfigValidator : AbstractValidator<HttpActionConfig>
{
    public HttpActionConfigValidator()
    {
        RuleFor(x => x.Method)
            .NotNull()
            .WithMessage("HTTP method is required.");

        RuleFor(x => x.Url)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) &&
                       (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("HTTP action URL must be an absolute http or https URL.");

        RuleFor(x => x.Headers)
            .NotNull()
            .WithMessage("Headers cannot be null.");

        RuleForEach(x => x.SuccessStatusCodes)
            .InclusiveBetween(100, 599)
            .WithMessage("Success status codes must be between 100 and 599.");
    }
}
