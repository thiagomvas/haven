using Haven.Application.Common.Interfaces;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Interceptors;

using Mediator;

using Microsoft.EntityFrameworkCore;

using NSubstitute;

namespace Haven.Testing.Common;

public static class TestDbContextFactory
{
    public static DbContextOptions<HavenDbContext> CreateInMemoryDbContextOptions()
    {
        var options = new DbContextOptionsBuilder<HavenDbContext>()
            .UseSqlite($"DataSource=file:memdb{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;

        return options;
    }

    public static HavenDbContext CreateUnitDbContext()
    {
        var options = CreateInMemoryDbContextOptions();
        var mediator = Substitute.For<IMediator>();
        var interceptor = new DomainEventInterceptor(mediator);
        var encryptionService = Substitute.For<IEncryptionService>();

        var context = new HavenDbContext(options, interceptor, encryptionService);
        context.Database.EnsureCreated();

        return context;
    }
}