using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

namespace Haven.Application.Features.DockerCleanup.Commands.UpdateDockerCleanupOptions;

[AdminOnly]
public sealed record UpdateDockerCleanupOptionsCommand(DockerCleanupOptions Options) : ICommand<DockerCleanupOptions>;