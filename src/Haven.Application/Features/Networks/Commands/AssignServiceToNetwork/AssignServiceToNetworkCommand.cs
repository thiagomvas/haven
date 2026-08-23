using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Networks.Commands.AssignServiceToNetwork;

[RequirePermission(Permissions.Dns.ManageNetworks)]
public sealed class AssignServiceToNetworkCommand : ICommand, IMutatesManifestState
{
    public Guid NetworkId { get; set; }
    public Guid ServiceId { get; set; }
}