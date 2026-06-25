using Haven.Application.Common.Interfaces;
using Haven.Application.Configuration;
using Haven.Application.Features.Backups.Commands.RestoreBackup;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.Backup;

public sealed class BackupManifestReader(
    IOptionsMonitor<BackupOptions> backupOptions,
    IOptionsMonitor<ManifestsOptions> manifestsOptions,
    ILogger<BackupManifestReader> logger) : IBackupManifestReader
{
    public async Task<string> PrepareSourceDirectoryAsync(
        RestoreSource source,
        string? snapshotName,
        string? commitSha,
        CancellationToken ct) => source switch
        {
            RestoreSource.FileSystem => PrepareFilesystemSource(snapshotName),
            RestoreSource.Git => await PrepareGitSource(commitSha!, ct),
            RestoreSource.Manifest => PrepareManifestSource(),
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };

    private string PrepareManifestSource()
    {
        var manifestsPath = manifestsOptions.CurrentValue.ManifestsPath;
        if (!Directory.Exists(manifestsPath))
            throw new DirectoryNotFoundException($"Manifests directory not found: {manifestsPath}");

        logger.LogInformation("Using local manifests directory at {Path}", manifestsPath);
        return manifestsPath;
    }

    private string PrepareFilesystemSource(string? snapshotName)
    {
        if (string.IsNullOrWhiteSpace(snapshotName))
            throw new ArgumentException("Snapshot name is required for file system restore.", nameof(snapshotName));

        var snapshotPath = Path.Combine(backupOptions.CurrentValue.BackupsPath, snapshotName);
        if (!Directory.Exists(snapshotPath))
            throw new DirectoryNotFoundException($"Snapshot directory not found: {snapshotPath}");

        logger.LogInformation("Using filesystem snapshot at {Path}", snapshotPath);
        return snapshotPath;
    }

    private async Task<string> PrepareGitSource(string commitSha, CancellationToken ct)
    {
        var manifestsPath = manifestsOptions.CurrentValue.ManifestsPath;
        var repoPath = Repository.Discover(manifestsPath);

        if (string.IsNullOrEmpty(repoPath))
            throw new InvalidOperationException($"No git repository found at {manifestsPath}");

        var tempDir = Path.Combine(Path.GetTempPath(), $"haven-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        logger.LogInformation("Extracting git commit {Sha} to {TempDir}", commitSha, tempDir);

        using var repo = new Repository(repoPath);
        var commit = repo.Lookup<Commit>(commitSha)
            ?? throw new InvalidOperationException($"Commit '{commitSha}' not found in repository.");

        await ExtractTreeAsync(repo, commit.Tree, string.Empty, tempDir, ct);

        logger.LogInformation("Git commit {Sha} extracted to {TempDir}", commitSha, tempDir);
        return tempDir;
    }

    private static async Task ExtractTreeAsync(Repository repo, Tree tree, string relativePath, string targetDir, CancellationToken ct)
    {
        foreach (var entry in tree)
        {
            ct.ThrowIfCancellationRequested();

            var entryRelativePath = string.IsNullOrEmpty(relativePath)
                ? entry.Name
                : Path.Combine(relativePath, entry.Name);

            if (entry.TargetType == TreeEntryTargetType.Tree)
                await ExtractTreeAsync(repo, (Tree)entry.Target, entryRelativePath, targetDir, ct);
            else if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                var filePath = Path.Combine(targetDir, entryRelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                await File.WriteAllTextAsync(filePath, ((Blob)entry.Target).GetContentText(), ct);
            }
        }
    }
}
