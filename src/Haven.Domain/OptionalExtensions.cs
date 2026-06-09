namespace Haven.Domain;

public static class OptionalExtensions
{
    public static Optional<T> ToOptional<T>(this T? value) where T : struct =>
        value.HasValue ? value.Value : Optional<T>.None;
}