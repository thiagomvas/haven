namespace Haven.Application.Common.Interfaces;

public interface IBackupManifestWriter
{
    Task WriteAllAsync(string targetBasePath, CancellationToken ct = default);
}
