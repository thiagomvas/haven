namespace Haven.Application.Features.Networks.Queries.ListNetworks;

public sealed class NetworkDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? EnvironmentId { get; set; }
    public string? EnvironmentName { get; set; }
    public string? Subnet { get; set; }
    public string? Gateway { get; set; }
    public int ServiceCount { get; set; }
    public List<NetworkServiceDto> Services { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public sealed class NetworkServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? IpAddress { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
}
