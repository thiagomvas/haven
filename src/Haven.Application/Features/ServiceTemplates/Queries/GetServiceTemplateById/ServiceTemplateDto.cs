namespace Haven.Application.Features.ServiceTemplates.Queries.GetServiceTemplateById;

public sealed class ServiceTemplateDto
{
    public string Id { get; set; } = null!;
    public string Version { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public string Category { get; set; } = null!;
    public List<TemplateInputFieldDto> Inputs { get; set; } = new();
}
