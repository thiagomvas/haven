using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Networks.Commands.DeleteNetwork;

[RequirePermission(Permissions.Dns.ManageNetworks)]
public sealed class DeleteNetworkCommand : ICommand, IMutatesManifestState
{
    public Guid NetworkId { get; set; }
}