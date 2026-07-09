using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

namespace Haven.Application.Features.Volumes.Commands.UpdateVolumesOptions;

[AdminOnly]
public sealed record UpdateVolumesOptionsCommand(VolumesOptions Options) : ICommand<VolumesOptions>;
