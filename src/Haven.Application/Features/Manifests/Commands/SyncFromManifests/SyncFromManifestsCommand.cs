using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Manifests.Commands.SyncFromManifests;

[AdminOnly]
public sealed record SyncFromManifestsCommand : ICommand;
