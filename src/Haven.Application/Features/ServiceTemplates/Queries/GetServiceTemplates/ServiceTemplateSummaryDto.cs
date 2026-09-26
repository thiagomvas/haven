namespace Haven.Application.Features.ServiceTemplates.Queries.GetServiceTemplates;

public sealed class ServiceTemplateSummaryDto
{
    public string Id { get; set; } = null!;
    public string Version { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public string Category { get; set; } = null!;
}
