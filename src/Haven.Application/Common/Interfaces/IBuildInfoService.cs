namespace Haven.Application.Common.Interfaces;

public interface IBuildInfoService
{
    Task<BuildInfo> GetAsync(CancellationToken ct = default);
}

public record BuildInfo
{
    public string Version { get; init; } = string.Empty;
    public string CommitSha { get; init; } = string.Empty;
    public string BuildDate { get; init; } = string.Empty;
    public string BuildSystem { get; init; } = string.Empty;
    public string DotNetVersion { get; init; } = string.Empty;
    public DatabaseBuildInfo Database { get; init; } = new();
    public DockerEngineBuildInfo DockerEngine { get; init; } = new();
}

public record DockerEngineBuildInfo
{
    public bool IsConnected { get; init; }
    public string? Version { get; init; }
}

public record DatabaseBuildInfo
{
    public string Provider { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
}