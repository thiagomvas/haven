using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Instance.Dtos;

namespace Haven.Application.Features.Instance.Commands.UpdateInstance;

[AdminOnly]
public sealed record UpdateInstanceCommand(string InstanceName, string Timezone, TimeFormat TimeFormat)
    : ICommand<InstanceDto>;
