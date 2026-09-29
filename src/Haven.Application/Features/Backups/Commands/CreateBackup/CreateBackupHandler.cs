using System.Globalization;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Domain;
using Haven.Domain.Enums;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Backups.Commands.CreateBackup;

public sealed class CreateBackupHandler(
    IBackupManifestWriter backupManifestWriter,
    IGitProviderFactory gitProviderFactory,
    IGitCredentialsRepository gitCredentialsRepository,
    IOptionsMonitor<BackupOptions> backupOptions,
    IOptionsMonitor<ManifestsOptions> manifestsOptions,
    IBackupCoordinationLock coordinationLock)
    : ICommandHandler<CreateBackupCommand, CreateBackupResult>
{
    public async ValueTask<Result<CreateBackupResult>> Handle(CreateBackupCommand request,
        CancellationToken cancellationToken)
    {
        if (!coordinationLock.TryAcquire(out var release))
            return Result<CreateBackupResult>.Failure(Error.BackupOperationInProgress);

        using var _ = release;

        var options = backupOptions.CurrentValue;
        var timestamp = DateTimeOffset.UtcNow;
        var snapshotPath = Path.Combine(options.BackupsPath, timestamp.ToString("yyyyMMdd-HHmmss"));

        await backupManifestWriter.WriteAllAsync(snapshotPath, cancellationToken);

        ApplyRetention(options, snapshotPath);

        var manifestsPath = manifestsOptions.CurrentValue.ManifestsPath;
        await backupManifestWriter.WriteAllAsync(manifestsPath, cancellationToken);

        if (options.Git.Enabled)
        {
            var credentials = options.Git.GitCredentialsId is not null
                ? await gitCredentialsRepository.GetByIdAsync(options.Git.GitCredentialsId.Value, cancellationToken)
                : null;

            var gitProvider =
                gitProviderFactory.Create(credentials?.ProviderType ?? GitProviderType.Generic, credentials);

            await gitProvider.InitRepositoryAsync(manifestsPath, cancellationToken);
            await gitProvider.CommitAsync(manifestsPath, $"backup: {timestamp:yyyyMMdd-HHmmss}", options.Git.Branch, cancellationToken);

            if (options.Git.RemoteUrl is not null && credentials is not null)
                await gitProvider.PushAsync(manifestsPath, options.Git.RemoteUrl, options.Git.Branch, cancellationToken);
        }

        return Result<CreateBackupResult>.CreatedFor(new CreateBackupResult(snapshotPath, timestamp));
    }

    private static void ApplyRetention(BackupOptions options, string currentSnapshotPath)
    {
        if (!Directory.Exists(options.BackupsPath))
            return;

        // Only directories named like snapshots take part in retention, so unrelated folders in the
        // backups path neither consume retention slots nor get deleted. The snapshot just written is
        // never a deletion candidate.
        var snapshots = Directory.GetDirectories(options.BackupsPath)
            .Where(dir => DateTime.TryParseExact(Path.GetFileName(dir), "yyyyMMdd-HHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .OrderByDescending(Path.GetFileName)
            .ToList();

        foreach (var snapshot in snapshots.Skip(Math.Max(options.RetentionCount, 1)))
        {
            if (Path.GetFullPath(snapshot) == Path.GetFullPath(currentSnapshotPath))
                continue;

            Directory.Delete(snapshot, recursive: true);
        }
    }
}
