using System.Text.Json;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Haven.Infrastructure.Persistence.Interceptors;

public sealed class DomainEventInterceptor(IMediator mediator) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken ct = default)
    {
        await DispatchDomainEventsAsync(eventData.Context, ct);
        return await base.SavedChangesAsync(eventData, result, ct);
    }

    private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken ct)
    {
        if (context is null) return;

        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        if (domainEvents.Count == 0) return;

        aggregates.ForEach(a => a.ClearDomainEvents());

        var auditEvents = domainEvents.Select(e =>
            Event.Create(e.GetType().Name, JsonSerializer.Serialize(e, e.GetType()))
        ).ToList();

        context.Set<Event>().AddRange(auditEvents);
        await context.SaveChangesAsync(ct);

        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent, ct);
    }
}