namespace Haven.Application.Common.Interfaces;

/// <summary>
/// Requests that the live manifests directory be resynced from current DB state. Implementations
/// debounce/coalesce bursts of requests into a single full resync rather than writing on every call.
/// </summary>
public interface IManifestSyncTrigger
{
    void RequestSync();
}