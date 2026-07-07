using Haven.Application.Common.Interfaces.Deployment;

using Microsoft.Extensions.Caching.Memory;

namespace Haven.Infrastructure.Deployment.Git;

public class MemoryOAuthStateStore(IMemoryCache cache) : IOAuthStateStore
{
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    public string GenerateState(Guid? credentialId = null)
    {
        var state = Convert.ToHexString(Guid.NewGuid().ToByteArray());
        cache.Set(CacheKey(state), credentialId, StateLifetime);
        return state;
    }

    public bool TryConsumeState(string state, out Guid? credentialId)
    {
        if (!cache.TryGetValue(CacheKey(state), out credentialId))
        {
            credentialId = null;
            return false;
        }

        cache.Remove(CacheKey(state));
        return true;
    }

    private static string CacheKey(string state) => $"github-oauth-state:{state}";
}
