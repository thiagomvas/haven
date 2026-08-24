namespace Haven.Application.Features.Configuration;

/// <summary>Never carries the password/hash - write-only via <c>UpdateTraefikDashboardAuthCommand</c>.</summary>
public sealed class TraefikDashboardAuthDto
{
    public bool Enabled { get; set; }
    public string? Username { get; set; }
}
