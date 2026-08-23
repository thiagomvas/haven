using Haven.Domain.Enums;

namespace Haven.Application.Features.Sidecars;

public sealed class SidecarDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Alias { get; set; }
    public SidecarKind Kind { get; set; }
    public ServiceStatus Status { get; set; }
    public ServiceHealth Health { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastDeployedAt { get; set; }
    public string? Image { get; set; }
    public List<string> Ports { get; set; } = [];
    public List<string> CommandArgs { get; set; } = [];
    public RestartPolicy? RestartPolicy { get; set; }
}