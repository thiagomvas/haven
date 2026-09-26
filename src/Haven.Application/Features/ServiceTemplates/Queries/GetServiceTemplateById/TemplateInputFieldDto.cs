using Haven.Application.Features.ServiceTemplates.Contracts;

namespace Haven.Application.Features.ServiceTemplates.Queries.GetServiceTemplateById;

public sealed class TemplateInputFieldDto
{
    public string Key { get; set; } = null!;
    public TemplateInputFieldType Type { get; set; }
    public string? Label { get; set; }
    public string? DefaultValue { get; set; }
    public bool Immutable { get; set; }
    public string[]? Options { get; set; }
}
