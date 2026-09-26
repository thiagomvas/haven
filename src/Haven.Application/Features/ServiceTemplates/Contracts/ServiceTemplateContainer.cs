namespace Haven.Application.Features.ServiceTemplates.Contracts;

public class ServiceTemplateContainer
{
    public string DockerImage { get; set; } = null!;
    public Dictionary<string, string> Env { get; set; } = new();
    public List<ServiceTemplateContainerVolume> Volumes { get; set; } = new();
    public List<string> CommandArgs { get; set; } = new();
}