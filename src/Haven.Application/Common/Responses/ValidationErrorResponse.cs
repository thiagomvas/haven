namespace Haven.Application.Common.Responses;

public record ValidationErrorResponse(
    bool Success,
    string Message,
    Dictionary<string, string[]> Errors)
{
    public static ValidationErrorResponse FromValidationFailures(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
    {
        var errorsByProperty = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        return new ValidationErrorResponse(
            false,
            "Validation failed",
            errorsByProperty);
    }
}
