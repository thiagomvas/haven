namespace Haven.Application.Features.Backups.Queries.ListSnapshots;

public sealed record SnapshotInfo(string Name, DateTimeOffset? CreatedAt);