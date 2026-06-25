using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Domain;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Backups.Commands.CreateBackup;

public sealed class CreateBackupHandler(
    IBackupManifestWriter backupManifestWriter,
    IGitProviderFactory gitProviderFactory,
    IGitCredentialsRepository gitCredentialsRepository,
    IOptionsMonitor<BackupOptions> backupOptions,
    IOptionsMonitor<ManifestsOptions> manifestsOptions)
    : ICommandHandler<CreateBackupCommand, CreateBackupResult>
{
    public async ValueTask<Result<CreateBackupResult>> Handle(CreateBackupCommand request,
        CancellationToken cancellationToken)
    {
        var options = backupOptions.CurrentValue;
        var timestamp = DateTimeOffset.UtcNow;
        var snapshotPath = Path.Combine(options.BackupsPath, timestamp.ToString("yyyyMMdd-HHmmss"));

        await backupManifestWriter.WriteAllAsync(snapshotPath, cancellationToken);

        ApplyRetention(options);

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

    private static void ApplyRetention(BackupOptions options)
    {
        if (!Directory.Exists(options.BackupsPath))
            return;

        var snapshots = Directory.GetDirectories(options.BackupsPath)
            .OrderDescending()
            .ToList();

        foreach (var snapshot in snapshots.Skip(options.RetentionCount))
            Directory.Delete(snapshot, recursive: true);
    }
}