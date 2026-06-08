using Haven.Application.Configuration;

namespace Haven.Application.Common.Interfaces;

public interface IHavenService
{
    Task<bool> RequiresFirstTimeSetupAsync(CancellationToken ct);
    Task<SetupStage> GetSetupStageAsync(CancellationToken ct);
    Task AdvanceSetupStageAsync(SetupStage stage, CancellationToken ct);
}
