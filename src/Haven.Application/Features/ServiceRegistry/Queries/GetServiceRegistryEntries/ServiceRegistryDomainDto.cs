namespace Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries;

public sealed class ServiceRegistryDomainDto
{
    public Guid Id { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public int ContainerPort { get; set; }
    public bool EnableTls { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}