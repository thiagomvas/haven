using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Exceptions;

using Shouldly;

namespace Haven.Domain.Tests.Entities;

[TestFixture]
[Category("Unit")]
public sealed class ServiceRegistryDomainTests
{
    [Test]
    public void Create_NormalizesHostnameToLowercaseAndTrims()
    {
        var entryId = Guid.NewGuid();

        var domain = ServiceRegistryDomain.Create(entryId, "  App.Example.COM  ", 8080);

        domain.ServiceRegistryEntryId.ShouldBe(entryId);
        domain.Hostname.ShouldBe("app.example.com");
        domain.ContainerPort.ShouldBe(8080);
    }

    [Test]
    public void Create_EmptyHostname_Throws()
    {
        Should.Throw<ValidationException>(() =>
            ServiceRegistryDomain.Create(Guid.NewGuid(), "   ", 8080));
    }

    [TestCase("not a hostname!!")]
    [TestCase("http://example.com")]
    public void Create_InvalidHostname_Throws(string hostname)
    {
        Should.Throw<ValidationException>(() =>
            ServiceRegistryDomain.Create(Guid.NewGuid(), hostname, 8080));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(65536)]
    public void Create_PortOutOfRange_Throws(int port)
    {
        Should.Throw<ValidationException>(() =>
            ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", port));
    }

    [Test]
    public void Create_DefaultsTlsModeToNone()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80);

        domain.TlsMode.ShouldBe(TlsMode.None);
    }

    [Test]
    public void Create_TlsModeAcme_SetsTlsMode()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80, tlsMode: TlsMode.Acme);

        domain.TlsMode.ShouldBe(TlsMode.Acme);
    }

    [Test]
    public void Apply_UpdatesAndRenormalizesHostname()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80);

        domain.Apply("New.Example.COM", Optional<int>.None);

        domain.Hostname.ShouldBe("new.example.com");
    }

    [Test]
    public void Apply_TlsModeProvided_UpdatesTlsMode()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80);

        domain.Apply(Optional<string>.None, Optional<int>.None, TlsMode.Acme);

        domain.TlsMode.ShouldBe(TlsMode.Acme);
    }

    [Test]
    public void Apply_TlsModeNotProvided_LeavesTlsModeUnchanged()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80, tlsMode: TlsMode.Acme);

        domain.Apply("new.example.com", Optional<int>.None);

        domain.TlsMode.ShouldBe(TlsMode.Acme);
    }

    [Test]
    public void Reconstitute_SetsAllFields()
    {
        var id = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var domain = ServiceRegistryDomain.Reconstitute(id, entryId, "example.com", 8080, TlsMode.Acme, now, now);

        domain.Id.ShouldBe(id);
        domain.ServiceRegistryEntryId.ShouldBe(entryId);
        domain.Hostname.ShouldBe("example.com");
        domain.ContainerPort.ShouldBe(8080);
        domain.TlsMode.ShouldBe(TlsMode.Acme);
        domain.CreatedAt.ShouldBe(now);
        domain.UpdatedAt.ShouldBe(now);
    }

    [Test]
    public void Create_InternalBasePathNotProvided_IsNull()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80);

        domain.InternalBasePath.ShouldBeNull();
    }

    [TestCase("/api/v1", "/api/v1")]
    [TestCase("/api/v1/", "/api/v1")]
    [TestCase("  /api/v1  ", "/api/v1")]
    public void Create_InternalBasePath_NormalizesTrailingSlashAndWhitespace(string input, string expected)
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80, internalBasePath: input);

        domain.InternalBasePath.ShouldBe(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("/")]
    public void Create_InternalBasePath_BlankOrRoot_CollapsesToNull(string? input)
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80, internalBasePath: input);

        domain.InternalBasePath.ShouldBeNull();
    }

    [TestCase("api/v1")]
    [TestCase("no-leading-slash")]
    public void Create_InternalBasePath_MissingLeadingSlash_Throws(string input)
    {
        Should.Throw<ValidationException>(() =>
            ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80, internalBasePath: input));
    }

    [Test]
    public void Apply_InternalBasePathProvided_UpdatesAndNormalizes()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80);

        domain.Apply(Optional<string>.None, Optional<int>.None, internalBasePath: "/api/v1/");

        domain.InternalBasePath.ShouldBe("/api/v1");
    }

    [Test]
    public void Apply_InternalBasePathExplicitEmpty_ClearsToNull()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80, internalBasePath: "/api/v1");

        domain.Apply(Optional<string>.None, Optional<int>.None, internalBasePath: "");

        domain.InternalBasePath.ShouldBeNull();
    }

    [Test]
    public void Apply_InternalBasePathNotProvided_LeavesUnchanged()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80, internalBasePath: "/api/v1");

        domain.Apply("new.example.com", Optional<int>.None);

        domain.InternalBasePath.ShouldBe("/api/v1");
    }
}