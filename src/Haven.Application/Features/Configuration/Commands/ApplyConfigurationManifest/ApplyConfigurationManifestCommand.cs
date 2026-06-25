using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Configuration.Commands.ApplyConfigurationManifest;

[AdminOnly]
public sealed class ApplyConfigurationManifestCommand : ICommand
{
    public string ManifestYaml { get; set; } = string.Empty;
}