namespace Haven.Domain;

public readonly struct Optional<T>
{
    private readonly T _value;

    public bool HasValue { get; }
    public T Value => HasValue ? _value : throw new InvalidOperationException("Optional has no value");

    private Optional(T value)
    {
        _value = value;
        HasValue = true;
    }

    public static Optional<T> None => default;
    public static implicit operator Optional<T>(T value) => new(value);
}