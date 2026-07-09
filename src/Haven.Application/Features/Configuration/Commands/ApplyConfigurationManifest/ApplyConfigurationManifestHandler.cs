using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Configuration.Commands.ApplyConfigurationManifest;

public sealed class ApplyConfigurationManifestHandler(
    IHavenConfigurationSerializer serializer,
    IHavenConfigurationSeedService seedService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ApplyConfigurationManifestCommand>
{
    public async ValueTask<Result> Handle(ApplyConfigurationManifestCommand request, CancellationToken cancellationToken)
    {
        if (!serializer.TryParse(request.ManifestYaml, out var error))
            return Error.Validation($"Invalid configuration YAML: {error}");

        var newConfig = serializer.Parse(request.ManifestYaml);

        await seedService.SeedFromAsync(newConfig, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await serializer.WriteRawAsync(request.ManifestYaml, cancellationToken);

        return Result.Success();
    }
}