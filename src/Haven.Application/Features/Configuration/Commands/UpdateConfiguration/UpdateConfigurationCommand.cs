using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Dtos;

namespace Haven.Application.Features.Configuration.Commands.UpdateConfiguration;

[AdminOnly]
public sealed record UpdateConfigurationCommand(ManifestsOptions Manifests) : ICommand<HavenConfigurationDto>;