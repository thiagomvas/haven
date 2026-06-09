namespace Haven.Domain.ValueObjects;

public sealed class EncryptedValue : ValueObject
{
    public string Value { get; }

    private EncryptedValue(string value) => Value = value;

    public static EncryptedValue From(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        return new(value);
    }

    public static implicit operator string(EncryptedValue v) => v.Value;
    public static implicit operator EncryptedValue(string v) => From(v);

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}