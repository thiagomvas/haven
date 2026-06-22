using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Configuration.Commands.ApplyConfigurationManifest;

public sealed class ApplyConfigurationManifestHandler(
    IHavenConfigurationSerializer serializer,
    IHavenConfigurationSeedService seedService)
    : ICommandHandler<ApplyConfigurationManifestCommand>
{
    public async ValueTask<Result> Handle(ApplyConfigurationManifestCommand request, CancellationToken cancellationToken)
    {
        if (!serializer.TryParse(request.ManifestYaml, out var error))
            return new Error("General.Validation", $"Invalid configuration YAML: {error}");

        await serializer.WriteRawAsync(request.ManifestYaml, cancellationToken);
        await seedService.SeedAsync(cancellationToken);

        return Result.Success();
    }
}
