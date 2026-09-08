namespace Haven.Application.Features.System.Queries.GetLatestVersion;

public sealed class LatestVersionDto
{
    public string CurrentVersion { get; init; } = string.Empty;
    public string LatestVersion { get; init; } = string.Empty;
    public bool IsUpdateAvailable { get; init; }
    public string? Name { get; init; }
    public string? HtmlUrl { get; init; }
    public string? Body { get; init; }
    public bool Prerelease { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
}
