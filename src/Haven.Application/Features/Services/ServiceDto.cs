using Haven.Domain;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.Services.Queries;

public sealed class ServiceDto
{
    public Guid Id { get; set; }
    public Guid EnvironmentId { get; set; }
    public string Name { get; set; }
    public string? Alias { get; set; }
    public ServiceType Type { get; set; }
    public ExposureMode ExposureMode { get; set; }
    public ServiceStatus Status { get; set; }
    public ServiceSourceConfig? SourceConfig { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string WebhookUrl { get; set; }
    
    
    public ServiceDto()
    {
        
        
    }
    
    public ServiceDto(
        Guid id,
        Guid environmentId,
        string name,
        ServiceType type,
        ExposureMode exposureMode,
        ServiceStatus status,
        ServiceSourceConfig? sourceConfig,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        EnvironmentId = environmentId;
        Name = name;
        Type = type;
        ExposureMode = exposureMode;
        Status = status;
        SourceConfig = sourceConfig;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

}
