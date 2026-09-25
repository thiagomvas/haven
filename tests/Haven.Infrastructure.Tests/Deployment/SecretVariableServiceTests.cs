using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Interceptors;
using Haven.Infrastructure.Security;

using Mediator;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment;

[TestFixture]
[Category("Integration")]
public sealed class SecretVariableServiceTests : IDisposable
{
    private HavenDbContext _context = null!;
    private SqliteConnection _connection = null!;
    private SecretVariableService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<HavenDbContext>()
            .UseSqlite(_connection)
            .Options;

        var encryptionService = new AesEncryptionService(
            Options.Create(new EncryptionOptions { Key = Convert.ToBase64String(new byte[32]) }));

        var mediator = Substitute.For<IMediator>();
        _context = new HavenDbContext(
            options,
            new DomainEventInterceptor(mediator),
            encryptionService);
        _context.Database.EnsureCreated();

        _sut = new SecretVariableService(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _connection?.Dispose();
    }

    public void Dispose() => TearDown();

    [Test(Description = "A secret with a value round-trips through encryption/decryption transparently")]
    public async Task GetSecretsAsEnvironmentVariablesForServiceAsync_WithValue_ReturnsDecryptedValue()
    {
        var serviceId = Guid.NewGuid();
        _context.Secrets.Add(new SecretVariable
        {
            ParentId = serviceId,
            ParentType = EnvironmentVariableParentType.Service,
            Key = "API_KEY",
            Value = EncryptedValue.From("super-secret")
        });
        await _context.SaveChangesAsync();

        var result = (await _sut.GetSecretsAsEnvironmentVariablesForServiceAsync(serviceId)).ToList();

        result.Count.ShouldBe(1);
        result[0].Key.ShouldBe("API_KEY");
        result[0].Value.ShouldBe("super-secret");
    }

    [Test(Description = "A secret with no value stored must not throw and should surface as empty, not attempt to re-decrypt or re-encrypt an empty string")]
    public async Task GetSecretsAsEnvironmentVariablesForServiceAsync_WithNullValue_ReturnsEmptyStringWithoutThrowing()
    {
        var serviceId = Guid.NewGuid();
        _context.Secrets.Add(new SecretVariable
        {
            ParentId = serviceId,
            ParentType = EnvironmentVariableParentType.Service,
            Key = "UNSET_KEY",
            Value = null
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetSecretsAsEnvironmentVariablesForServiceAsync(serviceId);

        var list = result.ToList();
        list.Count.ShouldBe(1);
        list[0].Key.ShouldBe("UNSET_KEY");
        list[0].Value.ShouldBe(string.Empty);
    }

    [Test(Description = "Only secrets scoped to the given service are returned")]
    public async Task GetSecretsAsEnvironmentVariablesForServiceAsync_OnlyReturnsSecretsForThatService()
    {
        var serviceId = Guid.NewGuid();
        var otherServiceId = Guid.NewGuid();
        _context.Secrets.AddRange(
            new SecretVariable
            {
                ParentId = serviceId,
                ParentType = EnvironmentVariableParentType.Service,
                Key = "KEY_A",
                Value = EncryptedValue.From("value-a")
            },
            new SecretVariable
            {
                ParentId = otherServiceId,
                ParentType = EnvironmentVariableParentType.Service,
                Key = "KEY_B",
                Value = EncryptedValue.From("value-b")
            },
            new SecretVariable
            {
                ParentId = serviceId,
                ParentType = EnvironmentVariableParentType.Project,
                Key = "KEY_C",
                Value = EncryptedValue.From("value-c")
            });
        await _context.SaveChangesAsync();

        var result = (await _sut.GetSecretsAsEnvironmentVariablesForServiceAsync(serviceId)).ToList();

        result.Count.ShouldBe(1);
        result[0].Key.ShouldBe("KEY_A");
    }
}
