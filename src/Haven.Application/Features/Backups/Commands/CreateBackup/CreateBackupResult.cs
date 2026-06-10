namespace Haven.Application.Features.Backups.Commands.CreateBackup;

public sealed record CreateBackupResult(string SnapshotPath, DateTimeOffset CreatedAt);
