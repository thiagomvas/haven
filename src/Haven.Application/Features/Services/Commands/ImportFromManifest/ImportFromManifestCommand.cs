using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.Services.Commands.ImportFromManifest;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class ImportFromManifestCommand : ICommand<Guid>, IMutatesManifestState
{
    public Guid EnvironmentId { get; set; }
    public string? RawManifest { get; set; }
}