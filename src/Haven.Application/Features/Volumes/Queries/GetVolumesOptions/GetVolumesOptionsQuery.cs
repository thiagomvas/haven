using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

namespace Haven.Application.Features.Volumes.Queries.GetVolumesOptions;

[AdminOnly]
public sealed record GetVolumesOptionsQuery : IQuery<VolumesOptions>;
