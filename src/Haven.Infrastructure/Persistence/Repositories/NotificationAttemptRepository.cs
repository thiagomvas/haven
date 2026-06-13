using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public sealed class NotificationAttemptRepository(HavenDbContext context) : INotificationAttemptRepository
{
    public Task<Guid> AddAsync(NotificationAttempt attempt, CancellationToken ct = default)
    {
        context.NotificationAttempts.Add(attempt);
        return Task.FromResult(attempt.Id);
    }

    public Task<NotificationAttempt?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => context.NotificationAttempts
            .Include(a => a.Rule)
                .ThenInclude(r => r!.ChannelConfig)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task UpdateAsync(NotificationAttempt attempt, CancellationToken ct = default)
    {
        context.NotificationAttempts.Update(attempt);
        return Task.CompletedTask;
    }
}
