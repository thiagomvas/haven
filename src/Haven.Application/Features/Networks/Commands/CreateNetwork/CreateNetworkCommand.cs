using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Networks.Commands.CreateNetwork;

[RequirePermission(Permissions.Dns.ManageNetworks)]
public sealed record CreateNetworkCommand(
    string Name,
    Guid? ProjectId = null,
    Guid? EnvironmentId = null,
    string? Metadata = null
) : ICommand<Guid>, IMutatesManifestState;