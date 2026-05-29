namespace Haven.Application.Features.System.Queries.GetBuildInfo;

public sealed class BuildInfoDto
{
    public string Version { get; init; } = string.Empty;
    public string CommitSha { get; init; } = string.Empty;
    public string BuildDate { get; init; } = string.Empty;
    public string BuildSystem { get; init; } = string.Empty;
    public string DotNetVersion { get; init; } = string.Empty;
    public DatabaseBuildInfoDto Database { get; init; } = new();
}

public sealed class DatabaseBuildInfoDto
{
    public string Provider { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
}
