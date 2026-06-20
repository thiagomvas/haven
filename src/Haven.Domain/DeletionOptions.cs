namespace Haven.Domain;

public sealed record DeletionOptions
{
    public bool RaiseEnvironmentDeletedEvents { get; init; }
    public bool RaiseServiceDeletedEvents { get; init; }

    public static readonly DeletionOptions Default = new();
    public static readonly DeletionOptions FullCascade = new()
    {
        RaiseEnvironmentDeletedEvents = true,
        RaiseServiceDeletedEvents = true
    };
}