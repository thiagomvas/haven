namespace Haven.Application.Features.Environments.Queries;

public sealed record EnvironmentDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Alias,
    string? Description,
    string NetworkName,
    int ServiceCount);
