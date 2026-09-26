using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain.Enums;

namespace Haven.Application.Features.ServiceTemplates.Commands.CreateServiceFromTemplate;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class CreateServiceFromTemplateCommand : ICommand<Guid>, IMutatesManifestState
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public string TemplateId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Alias { get; set; }
    public Dictionary<string, string> InputValues { get; set; } = new();
    public ExposureMode ExposureMode { get; set; } = ExposureMode.None;
    public List<string> Ports { get; set; } = new();
}
