using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Instance.Dtos;

namespace Haven.Application.Features.Instance.Queries.GetInstance;

[AdminOnly]
public sealed record GetInstanceQuery : IQuery<InstanceDto>;