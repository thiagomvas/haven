using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.Services.Commands.UpdateService;

public sealed class UpdateServiceCommand : ICommand<Guid>
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ServiceId { get; set; }
    public Optional<string> Name { get; set; }
    public Optional<ServiceType> Type { get; set; }
    public Optional<ExposureMode> ExposureMode { get; set; }
    public Optional<DockerConfig?> DockerConfig { get; set; }

    public Optional<ServiceSourceConfig?> ResolveSourceConfig()
    {
        if (!Type.HasValue && !DockerConfig.HasValue)
            return default;

        if (DockerConfig.HasValue)
            return (Optional<ServiceSourceConfig?>)DockerConfig.Value;

        return default;
    }
}
