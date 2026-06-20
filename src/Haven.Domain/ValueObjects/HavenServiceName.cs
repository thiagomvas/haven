using System.Text.RegularExpressions;

using Haven.Domain.Exceptions;

namespace Haven.Domain.ValueObjects;

public sealed class HavenServiceName : ValueObject
{
    public static readonly Regex ValidPattern = new(@"^[a-zA-Z0-9\s_-]+$", RegexOptions.Compiled);

    public string Value { get; }

    private HavenServiceName(string value) => Value = value;

    public static HavenServiceName From(string value)
    {
        if (string.IsNullOrEmpty(value) || !ValidPattern.IsMatch(value))
            throw new ValidationException($"'{value}' is not a valid service name. Only lowercase letters, digits, and hyphens are allowed.");

        return new(value);
    }

    public static implicit operator string(HavenServiceName n) => n.Value;

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}