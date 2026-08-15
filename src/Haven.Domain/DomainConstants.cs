namespace Haven.Domain;

public static class DomainConstants
{
    public const string NetworkBaseName = "haven";

    /// <summary>
    /// Name of the dedicated control-plane network every Sidecar auto-joins, independent of any
    /// Project/Environment network.
    /// </summary>
    public const string SystemNetworkName = "haven-system";

    public static string Slugify(string text)
    {
        return text.ToLowerInvariant()
            .Replace(" ", "-");
    }

}