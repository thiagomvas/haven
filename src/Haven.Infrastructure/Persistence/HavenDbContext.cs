using Haven.Application.Common.Interfaces;
using Haven.Domain.Aggregates;
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