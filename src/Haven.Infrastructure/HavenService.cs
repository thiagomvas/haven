using Haven.Application.Common.Interfaces;
using Haven.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure;

public class HavenService(HavenDbContext context) : IHavenService
{
    private static bool _requiresFirstTimeSetup = true;
    public async Task<bool> RequiresFirstTimeSetupAsync(CancellationToken ct)
    {
        if (!_requiresFirstTimeSetup) return false;

        var hasUsers = await context.Users.AnyAsync(ct);
        _requiresFirstTimeSetup = !hasUsers;
        return _requiresFirstTimeSetup;
    }
}