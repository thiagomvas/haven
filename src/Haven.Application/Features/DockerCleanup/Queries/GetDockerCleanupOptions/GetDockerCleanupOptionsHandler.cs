using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.DockerCleanup.Queries.GetDockerCleanupOptions;

public sealed class GetDockerCleanupOptionsHandler(IOptionsMonitor<DockerCleanupOptions> options)
    : IQueryHandler<GetDockerCleanupOptionsQuery, DockerCleanupOptions>
{
    public ValueTask<Result<DockerCleanupOptions>> Handle(GetDockerCleanupOptionsQuery request, CancellationToken ct)
        => ValueTask.FromResult(Result<DockerCleanupOptions>.Success(options.CurrentValue));
}