using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Configuration.Commands.UpdateTraefikDashboardAuth;

/// <summary>
/// Enables/disables and rotates HTTP Basic Auth credentials for the Traefik dashboard router.
/// <see cref="Password"/> is never round-tripped to the frontend - a blank/omitted value when
/// <see cref="Enabled"/> is true means "keep the existing password", matching the masked-password
/// convention used by the SMTP notification channel form.
/// </summary>
[RequirePermission(Permissions.Sidecars.Manage)]
public sealed class UpdateTraefikDashboardAuthCommand : ICommand<TraefikDashboardAuthDto>
{
    public bool Enabled { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
}