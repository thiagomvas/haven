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
    public DbSet<ServiceVolume> ServiceVolumes { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EnvironmentVariables> EnvironmentVariables { get; set; }
    public DbSet<HavenSetting> Settings { get; set; }
    public DbSet<FeatureFlag> FeatureFlags { get; set; }
    public DbSet<GitCredentials> GitCredentials { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<ServiceRegistryEntry> ServiceRegistryEntries { get; set; }
    public DbSet<NotificationRule> NotificationRules { get; set; }
    public DbSet<NotificationAttempt> NotificationAttempts { get; set; }
    public DbSet<NotificationChannelConfig> NotificationChannelConfigs { get; set; }
    public DbSet<Domain.Entities.Deployment> Deployments { get; set; }

    private readonly DomainEventInterceptor _domainEventInterceptor;
    private readonly IEncryptionService _encryptionService;
    private readonly List<Action> _postSaveActions = [];

    public HavenDbContext(
        DbContextOptions<HavenDbContext> options,
        DomainEventInterceptor domainEventInterceptor,
        IEncryptionService encryptionService)
        : base(options)
    {
        _domainEventInterceptor = domainEventInterceptor;
        _encryptionService = encryptionService;
    }

    public void OnAfterSave(Action action) => _postSaveActions.Add(action);

    public Task ReloadAsync<TEntity>(TEntity entity, CancellationToken ct = default) where TEntity : class =>
        Entry(entity).ReloadAsync(ct);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        var actions = _postSaveActions.ToList();
        _postSaveActions.Clear();
        foreach (var action in actions)
            action();

        return result;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_domainEventInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HavenDbContext).Assembly);

        var converter = new EncryptedValueConverter(_encryptionService);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(EncryptedValue))
                    property.SetValueConverter(converter);
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}