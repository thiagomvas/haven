namespace Haven.Application.Common.Interfaces;

public interface IHavenService
{
    Task<bool> RequiresFirstTimeSetupAsync(CancellationToken ct);
}