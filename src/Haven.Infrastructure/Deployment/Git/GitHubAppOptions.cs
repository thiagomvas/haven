namespace Haven.Infrastructure.Deployment.Git;

public sealed class GitHubAppOptions
{
    public const string SectionName = "GitHubApp";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}