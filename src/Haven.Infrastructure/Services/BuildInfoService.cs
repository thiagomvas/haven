using System.Data;
using System.Runtime.InteropServices;

using Docker.DotNet;

using Haven.Application.Common.Interfaces;
using Haven.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Haven.Infrastructure.Services;

public sealed class BuildInfoService(
    HavenDbContext dbContext,
    IConfiguration configuration,
    IDockerClient dockerClient) : IBuildInfoService
{
    public async Task<BuildInfo> GetAsync(CancellationToken ct = default)
    {
        var postgresVersion = await GetPostgresVersionAsync(ct);
        var dbPath = ExtractDbPath(configuration.GetConnectionString("DefaultConnection") ?? string.Empty);
        var dockerEngine = await GetDockerEngineInfoAsync(ct);

        return new BuildInfo
        {
            Version = Env("HAVEN_VERSION", "0.0.0"),
            CommitSha = Env("GIT_COMMIT", "unknown"),
            BuildDate = Env("BUILD_DATE", "unknown"),
            BuildSystem = Env("HAVEN_BUILD_SYSTEM", "binary"),
            DotNetVersion = RuntimeInformation.FrameworkDescription,
            Database = new DatabaseBuildInfo
            {
                Provider = "PostgreSQL",
                Version = postgresVersion,
                Path = dbPath,
            },
            DockerEngine = dockerEngine,
        };
    }

    private async Task<DockerEngineBuildInfo> GetDockerEngineInfoAsync(CancellationToken ct)
    {
        try
        {
            var version = await dockerClient.System.GetVersionAsync(ct);
            return new DockerEngineBuildInfo { IsConnected = true, Version = version.Version };
        }
        catch
        {
            return new DockerEngineBuildInfo { IsConnected = false };
        }
    }

    private async Task<string> GetPostgresVersionAsync(CancellationToken ct)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);

        using var command = connection.CreateCommand();
        command.CommandText = "SHOW server_version";
        return (string?)await command.ExecuteScalarAsync(ct) ?? "unknown";
    }

    private static string ExtractDbPath(string connectionString)
    {
        // Connection string format: "Host=host;Database=name;..."
        foreach (var segment in connectionString.Split(';'))
        {
            var parts = segment.Split('=', 2);
            if (parts.Length == 2 && parts[0].Trim().Equals("Database", StringComparison.OrdinalIgnoreCase))
                return parts[1].Trim();
        }
        return "unknown";
    }

    private static string Env(string key, string fallback) =>
        System.Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : fallback;
}