namespace Haven.Application.Common.Interfaces.Repositories;

public interface IHavenSettingRepository
{
    Task<string?> GetAsync(string category, CancellationToken ct);
    Task UpsertAsync(string category, string value, CancellationToken ct);
}