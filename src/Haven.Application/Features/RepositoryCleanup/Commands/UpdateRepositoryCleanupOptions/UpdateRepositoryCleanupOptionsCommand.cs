using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

namespace Haven.Application.Features.RepositoryCleanup.Commands.UpdateRepositoryCleanupOptions;

[AdminOnly]
public sealed record UpdateRepositoryCleanupOptionsCommand(RepositoryCleanupOptions Options) : ICommand<RepositoryCleanupOptions>;