using Haven.Application.Common.Interfaces;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Events;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Persistence.Converters;
using Haven.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Environment = Haven.Domain.Entities.Environment;


namespace Haven.Infrastructure.Persistence;

public class HavenDbContext : DbContext, IUnitOfWork
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Environment> Environments { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Network> Networks { get; set; }
    public DbSet<ServiceNetwork> ServiceNetworks { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EnvironmentVariables> EnvironmentVariables { get; set; }

    private readonly DomainEventInterceptor _domainEventInterceptor;
    private readonly SoftDeleteInterceptor _softDeleteInterceptor;
    private readonly IEncryptionService _encryptionService;

    public HavenDbContext(
        DbContextOptions<HavenDbContext> options,
        DomainEventInterceptor domainEventInterceptor,
        SoftDeleteInterceptor softDeleteInterceptor,
        IEncryptionService encryptionService)
        : base(options)
    {
        _domainEventInterceptor = domainEventInterceptor;
        _softDeleteInterceptor = softDeleteInterceptor;
        _encryptionService = encryptionService;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_softDeleteInterceptor, _domainEventInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HavenDbContext).Assembly);

        var converter = new EncryptedValueConverter(_encryptionService);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
                var nullCheck = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(null));
                var lambda = System.Linq.Expressions.Expression.Lambda(nullCheck, parameter);
                entityType.SetQueryFilter(lambda);
            }

            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(EncryptedValue))
                    property.SetValueConverter(converter);
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}