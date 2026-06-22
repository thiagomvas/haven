using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Configuration.Queries.GetConfigurationManifest;

[AdminOnly]
public sealed record GetConfigurationManifestQuery : IQuery<string>;
