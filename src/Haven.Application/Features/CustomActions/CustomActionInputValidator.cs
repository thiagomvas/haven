using System.Text.RegularExpressions;

using FluentValidation;

using Haven.Domain.Entities;

namespace Haven.Application.Features.CustomActions;

public sealed partial class CustomActionInputValidator : AbstractValidator<CustomActionInput>
{
    [GeneratedRegex(@"^[a-zA-Z0-9_-]+$")]
    private static partial Regex NamePattern();

    public CustomActionInputValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrEmpty(n) && NamePattern().IsMatch(n))
            .WithMessage("Input name must only contain letters, digits, '_' and '-'.");

        RuleFor(x => x.Label)
            .NotEmpty()
            .WithMessage("Input label cannot be empty.");
    }
}

public sealed class CustomActionInputsValidator : AbstractValidator<CustomActionInput[]>
{
    public CustomActionInputsValidator()
    {
        RuleForEach(x => x).SetValidator(new CustomActionInputValidator());

        RuleFor(x => x)
            .Must(inputs => inputs.Select(i => i.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == inputs.Length)
            .WithMessage("Input names must be unique.");
    }
}