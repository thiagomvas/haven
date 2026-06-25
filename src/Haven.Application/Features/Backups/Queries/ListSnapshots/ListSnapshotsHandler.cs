using System.Globalization;

using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Backups.Queries.ListSnapshots;

public sealed class ListSnapshotsHandler(IOptionsMonitor<BackupOptions> backupOptions)
    : IQueryHandler<ListSnapshotsQuery, IReadOnlyList<SnapshotInfo>>
{
    public ValueTask<Result<IReadOnlyList<SnapshotInfo>>> Handle(ListSnapshotsQuery request, CancellationToken ct)
    {
        var options = backupOptions.CurrentValue;
        if (!Directory.Exists(options.BackupsPath))
            return ValueTask.FromResult(Result<IReadOnlyList<SnapshotInfo>>.Success([]));

        var snapshots = Directory.GetDirectories(options.BackupsPath)
            .Select(dir =>
            {
                var name = Path.GetFileName(dir);
                var parsed = DateTimeOffset.TryParseExact(
                    name, "yyyyMMdd-HHmmss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out var dt);
                return new SnapshotInfo(name, parsed ? dt : null);
            })
            .OrderByDescending(s => s.Name)
            .ToList();

        return ValueTask.FromResult(Result<IReadOnlyList<SnapshotInfo>>.Success(snapshots));
    }
}
