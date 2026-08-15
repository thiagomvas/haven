using System.Text.RegularExpressions;

using Haven.Domain.Exceptions;

namespace Haven.Domain.ValueObjects;

public sealed class HavenServiceName : ValueObject
{
    public static readonly Regex ValidPattern = new(@"^[a-zA-Z0-9\s_-]+$", RegexOptions.Compiled);

    /// <summary>
    /// Names reserved for Haven's own use, shared by both <see cref="Aggregates.Service"/> and
    /// <see cref="Aggregates.Sidecar"/> so the two container kinds can never collide.
    /// </summary>
    public static readonly HashSet<string> ReservedNames =
        new(StringComparer.OrdinalIgnoreCase) { "haven", "dns", "localhost", "host", "internal" };

    public string Value { get; }

    private HavenServiceName(string value) => Value = value;

    public static HavenServiceName From(string value)
    {
        if (string.IsNullOrEmpty(value) || !ValidPattern.IsMatch(value))
            throw new ValidationException($"'{value}' is not a valid service name. Only lowercase letters, digits, and hyphens are allowed.");

        return new(value);
    }

    /// <summary>
    /// Validates the name is well-formed and not reserved. Throws <see cref="ValidationException"/> otherwise.
    /// </summary>
    public static void EnsureValidAndNotReserved(string value)
    {
        _ = From(value);

        if (ReservedNames.Contains(value))
            throw new ValidationException($"'{value}' is a reserved name and cannot be used.");
    }

    public static implicit operator string(HavenServiceName n) => n.Value;

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}