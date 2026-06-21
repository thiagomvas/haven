namespace Haven.Application.Features.Services.Queries.ResolveService;

public sealed class ServiceLocationDto
{
    public Guid ServiceId { get; init; }
    public Guid EnvironmentId { get; init; }
    public Guid ProjectId { get; init; }
}
