using System.Text.Json;
using System.Text.Json.Serialization;

using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

using Mediator;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Haven.Infrastructure.Persistence.Interceptors;

public sealed class DomainEventInterceptor(IMediator mediator) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Capture deleted aggregates before EF clears their tracking state
    private readonly List<AggregateRoot> _pendingDeletes = [];
    private bool _dispatching;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, ct);

        // Snapshot deleted aggregates that have domain events before EF removes them
        _pendingDeletes.AddRange(
            eventData.Context.ChangeTracker
                .Entries<AggregateRoot>()
                .Where(e => e.State == EntityState.Deleted && e.Entity.DomainEvents.Any())
                .Select(e =>
                {
                    // Detach so EF skips them in this save round
                    e.State = EntityState.Detached;
                    return e.Entity;
                })
        );

        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken ct = default)
    {
        if (!_dispatching)
            await DispatchDomainEventsAsync(eventData.Context, ct);
        return await base.SavedChangesAsync(eventData, result, ct);
    }

    private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken ct)
    {
        if (context is null) return;
        _dispatching = true;

        var activeAggregates = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var allAggregates = activeAggregates.Concat(_pendingDeletes).ToList();

        var domainEvents = allAggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        if (domainEvents.Count == 0)
        {
            _pendingDeletes.Clear();
            _dispatching = false;
            return;
        }

        allAggregates.ForEach(a => a.ClearDomainEvents());

        var auditEvents = domainEvents.Select(e =>
            Event.Create(e.GetType().Name, e.ToMessage(), JsonSerializer.Serialize(e, e.GetType(), JsonOptions))
        ).ToList();

        context.Set<Event>().AddRange(auditEvents);
        await context.SaveChangesAsync(ct);

        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent, ct);

        foreach (var aggregate in _pendingDeletes)
        {
            var trackedEntry = context.ChangeTracker.Entries<AggregateRoot>()
                .FirstOrDefault(e => e.Entity.Id == aggregate.Id);

            if (trackedEntry is not null)
                trackedEntry.State = EntityState.Deleted;
            else
                context.Entry(aggregate).State = EntityState.Deleted;
        }

        _pendingDeletes.Clear();

        await context.SaveChangesAsync(ct);

        _dispatching = false;
    }
}