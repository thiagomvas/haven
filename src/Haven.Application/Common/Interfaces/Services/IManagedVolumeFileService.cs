using Haven.Application.Common;

namespace Haven.Application.Common.Interfaces.Services;

/// <summary>
/// Reads and writes the files backing a Haven-managed volume. All operations are sandboxed to
/// the volume's directory (<c>{VolumesRoot}/{serviceId}/{volumeId}</c>); any path that would
/// escape that directory is rejected.
/// </summary>
public interface IManagedVolumeFileService
{
    /// <summary>
    /// Lists every file and directory under the managed volume, as paths relative to
    /// the volume root (using <c>/</c> separators). Returns an empty list if nothing exists yet.
    /// </summary>
    Task<Result<IReadOnlyList<ManagedVolumeFileEntry>>> ListFilesAsync(Guid serviceId, Guid volumeId,
        CancellationToken ct = default);

    /// <summary>
    /// Reads the text content of a file within the managed volume.
    /// </summary>
    Task<Result<string>> ReadFileAsync(Guid serviceId, Guid volumeId, string relativePath,
        CancellationToken ct = default);

    /// <summary>
    /// Creates or overwrites a file within the managed volume, creating parent
    /// directories as needed.
    /// </summary>
    Task<Result> WriteFileAsync(Guid serviceId, Guid volumeId, string relativePath, string content,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a file or subdirectory within the managed volume.
    /// </summary>
    Task<Result> DeleteFileAsync(Guid serviceId, Guid volumeId, string relativePath, CancellationToken ct = default);

    /// <summary>
    /// Deletes the entire managed volume directory (e.g. when the volume is removed).
    /// </summary>
    Task<Result> DeleteVolumeDirectoryAsync(Guid serviceId, Guid volumeId, CancellationToken ct = default);
}