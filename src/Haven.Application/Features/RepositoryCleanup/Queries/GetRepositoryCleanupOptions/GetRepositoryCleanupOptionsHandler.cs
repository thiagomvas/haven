using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.RepositoryCleanup.Queries.GetRepositoryCleanupOptions;

public sealed class GetRepositoryCleanupOptionsHandler(IOptionsMonitor<RepositoryCleanupOptions> options)
    : IQueryHandler<GetRepositoryCleanupOptionsQuery, RepositoryCleanupOptions>
{
    public ValueTask<Result<RepositoryCleanupOptions>> Handle(GetRepositoryCleanupOptionsQuery request, CancellationToken ct)
        => ValueTask.FromResult(Result<RepositoryCleanupOptions>.Success(options.CurrentValue));
}