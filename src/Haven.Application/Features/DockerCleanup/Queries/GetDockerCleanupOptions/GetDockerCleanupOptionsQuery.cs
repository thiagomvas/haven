using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

namespace Haven.Application.Features.DockerCleanup.Queries.GetDockerCleanupOptions;

[AdminOnly]
public sealed record GetDockerCleanupOptionsQuery : IQuery<DockerCleanupOptions>;