namespace Haven.Domain.Models;

public sealed record EnvironmentData(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Description,
    string NetworkName,
    IEnumerable<ServiceData>? Services = null);