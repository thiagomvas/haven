namespace Haven.Application.Features.Environments.Queries;

public sealed record EnvironmentDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Description,
    string NetworkName,
    int ServiceCount);
