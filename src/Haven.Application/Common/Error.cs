namespace Haven.Application.Common;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static readonly Error NotFound = new("General.NotFound", "The requested resource was not found.");
    public static readonly Error Conflict = new("General.Conflict", "A resource with that name already exists.");
    public static readonly Error Validation = new("General.Validation", "One or more validation errors occurred.");

    public static readonly Error Unauthorized =
        new("General.Unauthorized", "You are not authorised to perform this action.");

    public static Error NotFoundFor(string resource, Guid id) => new("NotFound", $"{resource} '{id}' was not found.");

    public static Error ConflictFor(string resource, string name) =>
        new("Conflict", $"{resource} '{name}' already exists.");
    
    public static implicit operator Result(Error error) => Result.Failure(error);
}