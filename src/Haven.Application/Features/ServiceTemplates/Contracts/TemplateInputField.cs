namespace Haven.Application.Features.ServiceTemplates.Contracts;

public record TemplateInputField
{
    public string Key { get; set; } = null!;
    public TemplateInputFieldType Type { get; set; }
    public string? Label { get; set; }
    public string? DefaultValue { get; set; }
    public bool Immutable { get; set; } = false;
    public string[]? Options { get; set; }
}