namespace Haven.Application.Features.ServiceTemplates.Contracts;

public class ServiceTemplateOutput
{
    public string Key { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string Value { get; set; } = null!;
    public bool Secret { get; set; }
}
