namespace Haven.Domain;

public static class DomainConstants
{
    public const string NetworkBaseName = "haven";

    public static string Slugify(string text)
    {
        return text.ToLowerInvariant()
            .Replace(" ", "-");
    }

}