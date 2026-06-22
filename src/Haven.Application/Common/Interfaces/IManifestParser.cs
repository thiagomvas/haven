namespace Haven.Application.Common.Interfaces;

public interface IManifestParser<TManifestDto>
{
    Task<TManifestDto> ParseAsync(string yaml, CancellationToken ct = default);
}
