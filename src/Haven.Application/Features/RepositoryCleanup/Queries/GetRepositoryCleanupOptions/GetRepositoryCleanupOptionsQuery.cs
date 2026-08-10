using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

namespace Haven.Application.Features.RepositoryCleanup.Queries.GetRepositoryCleanupOptions;

[AdminOnly]
public sealed record GetRepositoryCleanupOptionsQuery : IQuery<RepositoryCleanupOptions>;