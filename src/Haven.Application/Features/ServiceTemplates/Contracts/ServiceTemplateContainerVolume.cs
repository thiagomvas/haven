namespace Haven.Application.Features.ServiceTemplates.Contracts;

public class ServiceTemplateContainerVolume
{
    public string Name { get; set; } = null!;
    public string Mount { get; set; } = null!;
    public ServiceTemplateVolumeType Type { get; set; } = ServiceTemplateVolumeType.Named;

    /// <summary>
    /// Files seeded into the volume on first creation. Only meaningful when
    /// <see cref="Type"/> is <see cref="ServiceTemplateVolumeType.Managed"/>.
    /// </summary>
    public List<ServiceTemplateVolumeFile> Files { get; set; } = new();
}