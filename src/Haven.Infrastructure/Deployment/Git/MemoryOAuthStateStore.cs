using Haven.Application.Common.Interfaces.Deployment;

using Microsoft.Extensions.Caching.Memory;

namespace Haven.Infrastructure.Deployment.Git;

public class MemoryOAuthStateStore(IMemoryCache cache) : IOAuthStateStore
{
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    public string GenerateState()
    {
        var state = Convert.ToHexString(Guid.NewGuid().ToByteArray());
        cache.Set(CacheKey(state), true, StateLifetime);
        return state;
    }

    public bool TryConsumeState(string state)
    {
        if (!cache.TryGetValue(CacheKey(state), out _))
            return false;

        cache.Remove(CacheKey(state));
        return true;
    }

    private static string CacheKey(string state) => $"github-oauth-state:{state}";
}
