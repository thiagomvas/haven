namespace Haven.Application.Configuration;

public class VolumesOptions
{
    public const string SectionName = "Volumes";

    /// <summary>
    /// Root directory on the host where Haven-managed volume files live. Each managed volume
    /// occupies <c>{RootPath}/{serviceId}/{volumeId}</c>, which is bind-mounted into the
    /// container at deploy time. Must be a path the Docker daemon can bind-mount.
    /// </summary>
    public string RootPath { get; set; } = "volumes";
}
