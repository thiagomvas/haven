using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Configuration.Queries.GetTraefikDashboardAuth;

[RequirePermission(Permissions.Sidecars.Read)]
public sealed record GetTraefikDashboardAuthQuery : IQuery<TraefikDashboardAuthDto>;