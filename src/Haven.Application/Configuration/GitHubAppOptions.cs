namespace Haven.Application.Configuration;

public sealed class GitHubAppOptions
{
    public const string SectionName = "githubApp";
    public const string CallbackPath = "/api/github/oauth/callback";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}