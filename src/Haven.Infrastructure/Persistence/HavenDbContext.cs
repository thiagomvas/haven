using Haven.Application.Common.Interfaces;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Events;
using Haven.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence;

public class HavenDbContext : DbContext, IUnitOfWork
{
    public DbSet<Project> Projects { get; set; }

    private readonly DomainEventInterceptor _domainEventInterceptor;

    public HavenDbContext(DbContextOptions<HavenDbContext> options, DomainEventInterceptor domainEventInterceptor)
        : base(options)
    {
        _domainEventInterceptor = domainEventInterceptor;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TrackNewOwnedEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void TrackNewOwnedEntities()
    {
        // Calling Entries() triggers DetectChanges, which discovers new owned entities
        // in OwnsMany collections but incorrectly assigns Modified instead of Added.
        // We read domain events to find those new entities and force state to Added.
        var newEntities = ChangeTracker
            .Entries<AggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents.OfType<IEntityCreatedEvent>())
            .Select(e => e.CreatedEntity)
            .ToList();

        foreach (Entity entity in newEntities)
        {
            var entry = Entry(entity);
            if (entry.State != EntityState.Added)
                entry.State = EntityState.Added;
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_domainEventInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HavenDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}