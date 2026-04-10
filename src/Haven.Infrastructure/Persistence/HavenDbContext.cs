using Haven.Application.Common.Interfaces;
using Haven.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence;

public class HavenDbContext : DbContext, IUnitOfWork
{
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
}