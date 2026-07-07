namespace Haven.Application.Common.Interfaces.Services;

/// <summary>A file or directory within a managed volume.</summary>
public sealed record ManagedVolumeFileEntry(string Path, bool IsDirectory, long Size);
