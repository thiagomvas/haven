using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Configuration.Dtos;

namespace Haven.Application.Features.Configuration.Queries.GetConfiguration;

[AdminOnly]
public sealed record GetConfigurationQuery : IQuery<HavenConfigurationDto>;