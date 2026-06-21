namespace Haven.Application.Features.Services.Queries;

public sealed class PortMappingDto
{
    public int? HostPort { get; set; }
    public int ContainerPort { get; set; }
    public string? IpAddress { get; set; }
}