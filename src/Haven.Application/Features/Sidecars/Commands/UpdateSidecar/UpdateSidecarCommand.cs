using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.Sidecars.Commands.UpdateSidecar;

[RequirePermission(Permissions.Sidecars.Manage)]
public sealed class UpdateSidecarCommand : ICommand<Guid>, IMutatesManifestState
{
    public Guid SidecarId { get; set; }
    public Optional<DockerConfig?> DockerConfig { get; set; }
}