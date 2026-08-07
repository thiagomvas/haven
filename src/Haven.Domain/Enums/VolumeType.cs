namespace Haven.Domain.Enums;

public enum VolumeType
{
    /// <summary>
    /// A Docker named volume, referenced by name and managed by the Docker daemon.
    /// </summary>
    Named,

    /// <summary>
    /// A bind mount of an absolute path on the host filesystem.
    /// </summary>
    HostPath,

    /// <summary>
    /// A Haven-owned directory whose files are authored and managed through Haven,
    /// bind-mounted into the container at deploy time.
    /// </summary>
    Managed
}