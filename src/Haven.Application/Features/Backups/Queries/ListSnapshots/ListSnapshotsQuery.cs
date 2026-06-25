using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Backups.Queries.ListSnapshots;

[RequirePermission(Permissions.System.ManageBackups)]
public sealed record ListSnapshotsQuery : IQuery<IReadOnlyList<SnapshotInfo>>;