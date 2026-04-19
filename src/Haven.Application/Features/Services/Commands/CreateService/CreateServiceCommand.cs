using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.Services.Commands.CreateService;

public sealed class CreateServiceCommand : ICommand<Guid>
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ServiceType Type { get; set; }
    public ExposureMode ExposureMode { get; set; }
}
