namespace Haven.Application.Features.Networks.Queries.SearchAttachableServices;

public sealed class AttachableServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Status { get; set; } = default!;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = default!;
    public Guid EnvironmentId { get; set; }
    public string EnvironmentName { get; set; } = default!;
}
