using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.ImportFromManifest;

public class ImportFromManifestCommand : ICommand<Guid>
{
    public Guid EnvironmentId { get; set; }
    public string? RawManifest { get; set; }

}