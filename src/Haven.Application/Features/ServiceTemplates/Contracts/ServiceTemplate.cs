using Version = Haven.Domain.ValueObjects.Version;

namespace Haven.Application.Features.ServiceTemplates.Contracts;

public class ServiceTemplate
{
    public string Id { get; set; } = null!;
    public Version Version { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public string Category { get; set; } = null!;
}