namespace Haven.Application.Features.ServiceTemplates.Contracts;

public class ServiceTemplateContainer
{
    public string DockerImage { get; set; } = null!;
    public List<ServiceTemplateContainerVolume> Volumes { get; set; } = new();
}