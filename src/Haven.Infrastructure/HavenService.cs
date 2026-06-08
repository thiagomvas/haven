using System.Text.Json;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;

namespace Haven.Infrastructure;

public class HavenService(IHavenSettingRepository repository, IHavenConfigurationStore store) : IHavenService
{
    public async Task<bool> RequiresFirstTimeSetupAsync(CancellationToken ct)
    {
        var stage = await GetSetupStageAsync(ct);
        return stage < SetupStage.Completed;
    }

    public async Task<SetupStage> GetSetupStageAsync(CancellationToken ct)
    {
        var json = await repository.GetAsync(SetupOptions.SectionName, ct);
        if (json is null)
            return SetupStage.NotStarted;

        var options = JsonSerializer.Deserialize<SetupOptions>(json);
        return options?.Stage ?? SetupStage.NotStarted;
    }

    public async Task AdvanceSetupStageAsync(SetupStage stage, CancellationToken ct)
    {
        var options = new SetupOptions { Stage = stage };
        var json = JsonSerializer.Serialize(options);
        await repository.UpsertAsync(SetupOptions.SectionName, json, ct);
        store.Invalidate(SetupOptions.SectionName);
    }
}
