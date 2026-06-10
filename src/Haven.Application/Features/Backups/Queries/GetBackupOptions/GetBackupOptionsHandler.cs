using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Backups.Queries.GetBackupOptions;

public sealed class GetBackupOptionsHandler(IOptionsMonitor<BackupOptions> options)
    : IQueryHandler<GetBackupOptionsQuery, BackupOptions>
{
    public ValueTask<Result<BackupOptions>> Handle(GetBackupOptionsQuery request, CancellationToken ct)
        => ValueTask.FromResult(Result<BackupOptions>.Success(options.CurrentValue));
}
