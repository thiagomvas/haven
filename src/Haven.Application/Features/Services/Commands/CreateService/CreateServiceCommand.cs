using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.Services.Commands.CreateService;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class CreateServiceCommand : ICommand<Guid>
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public ServiceType Type { get; set; }
    public ExposureMode ExposureMode { get; set; }
    public DockerConfig? DockerConfig { get; set; }
    public DockerfileConfig? DockerfileConfig { get; set; }

    public ServiceSourceConfig? ResolveSourceConfig() => Type switch
    {
        ServiceType.DockerImage => DockerConfig,
        ServiceType.Dockerfile => DockerfileConfig,
        _ => null
    };
}