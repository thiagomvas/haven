using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Configuration;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.Persistence.Volumes;

public sealed class ManagedVolumeFileService(
    IOptionsMonitor<VolumesOptions> options,
    ILogger<ManagedVolumeFileService> logger) : IManagedVolumeFileService
{
    public Task<Result<IReadOnlyList<ManagedVolumeFileEntry>>> ListFilesAsync(Guid serviceId, Guid volumeId, CancellationToken ct = default)
    {
        var volumeRoot = VolumeRoot(serviceId, volumeId);
        var entries = new List<ManagedVolumeFileEntry>();

        if (Directory.Exists(volumeRoot))
        {
            foreach (var path in Directory.EnumerateFileSystemEntries(volumeRoot, "*", SearchOption.AllDirectories))
            {
                var isDirectory = Directory.Exists(path);
                var relative = Path.GetRelativePath(volumeRoot, path).Replace(Path.DirectorySeparatorChar, '/');
                var size = isDirectory ? 0L : new FileInfo(path).Length;
                entries.Add(new ManagedVolumeFileEntry(relative, isDirectory, size));
            }
        }

        IReadOnlyList<ManagedVolumeFileEntry> ordered = entries
            .OrderBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<ManagedVolumeFileEntry>>.Success(ordered));
    }

    public async Task<Result<string>> ReadFileAsync(Guid serviceId, Guid volumeId, string relativePath, CancellationToken ct = default)
    {
        if (!TryResolve(serviceId, volumeId, relativePath, out var fullPath))
            return Error.Validation("Invalid file path.");

        if (!File.Exists(fullPath))
            return Error.NotFound;

        var content = await File.ReadAllTextAsync(fullPath, ct);
        return content;
    }

    public async Task<Result> WriteFileAsync(Guid serviceId, Guid volumeId, string relativePath, string content, CancellationToken ct = default)
    {
        if (!TryResolve(serviceId, volumeId, relativePath, out var fullPath))
            return Error.Validation("Invalid file path.");

        if (Directory.Exists(fullPath))
            return Error.Validation("The target path is a directory.");

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(fullPath, content, ct);
        logger.LogDebug("Wrote managed volume file '{Path}' for volume {VolumeId}", relativePath, volumeId);
        return Result.Success();
    }

    public Task<Result> DeleteFileAsync(Guid serviceId, Guid volumeId, string relativePath, CancellationToken ct = default)
    {
        if (!TryResolve(serviceId, volumeId, relativePath, out var fullPath))
            return Task.FromResult<Result>(Error.Validation("Invalid file path."));

        if (File.Exists(fullPath))
            File.Delete(fullPath);
        else if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, recursive: true);
        else
            return Task.FromResult<Result>(Error.NotFound);

        logger.LogDebug("Deleted managed volume path '{Path}' for volume {VolumeId}", relativePath, volumeId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteVolumeDirectoryAsync(Guid serviceId, Guid volumeId, CancellationToken ct = default)
    {
        var volumeRoot = VolumeRoot(serviceId, volumeId);

        if (Directory.Exists(volumeRoot))
        {
            Directory.Delete(volumeRoot, recursive: true);
            logger.LogDebug("Deleted managed volume directory for volume {VolumeId}", volumeId);
        }

        return Task.FromResult(Result.Success());
    }

    private string VolumeRoot(Guid serviceId, Guid volumeId) =>
        DockerUtils.ManagedVolumeHostPath(options.CurrentValue.RootPath, serviceId, volumeId);

    /// <summary>
    /// Resolves a relative path against the volume root and verifies it does not escape
    /// that root, both syntactically (guards against path traversal via <c>..</c> or absolute
    /// paths) and via symlinks (the volume directory is bind-mounted into the running container,
    /// which could plant a symlink inside it pointing outside the volume root).
    /// </summary>
    private bool TryResolve(Guid serviceId, Guid volumeId, string relativePath, out string fullPath)
    {
        var volumeRoot = VolumeRoot(serviceId, volumeId);
        fullPath = Path.GetFullPath(Path.Combine(volumeRoot, relativePath));

        var rootWithSeparator = volumeRoot.EndsWith(Path.DirectorySeparatorChar)
            ? volumeRoot
            : volumeRoot + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal))
            return false;

        return !EscapesRootViaSymlink(volumeRoot, rootWithSeparator, fullPath);
    }

    /// <summary>
    /// Walks every existing path segment between <paramref name="volumeRoot"/> and
    /// <paramref name="fullPath"/> and returns true if any segment is a symlink/reparse point
    /// whose final target resolves outside the volume root.
    /// </summary>
    private static bool EscapesRootViaSymlink(string volumeRoot, string rootWithSeparator, string fullPath)
    {
        var relative = Path.GetRelativePath(volumeRoot, fullPath);
        if (relative == ".")
            return false;

        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var current = volumeRoot;

        foreach (var segment in segments)
        {
            current = Path.Combine(current, segment);

            if (!File.Exists(current) && !Directory.Exists(current))
                continue; // Doesn't exist yet (e.g. a new file being written) - nothing to resolve.

            FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
            var target = info.ResolveLinkTarget(returnFinalTarget: true)?.FullName;

            if (target is not null && target != volumeRoot && !target.StartsWith(rootWithSeparator, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}