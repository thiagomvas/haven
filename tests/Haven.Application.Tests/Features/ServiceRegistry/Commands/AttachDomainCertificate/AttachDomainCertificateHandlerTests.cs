using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.ServiceRegistry.Commands.AttachDomainCertificate;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.ServiceRegistry.Commands.AttachDomainCertificate;

[Category("Unit")]
public sealed class AttachDomainCertificateHandlerTests
{
    private const string ValidCertPem = """
                                         -----BEGIN CERTIFICATE-----
                                         MIIDNjCCAh6gAwIBAgIUErbYPB19kek5+gs04UPSr+FTjScwDQYJKoZIhvcNAQEL
                                         BQAwFjEUMBIGA1UEAwwLZXhhbXBsZS5jb20wHhcNMjYwODI0MDAxNTQ1WhcNMjcw
                                         ODI0MDAxNTQ1WjAWMRQwEgYDVQQDDAtleGFtcGxlLmNvbTCCASIwDQYJKoZIhvcN
                                         AQEBBQADggEPADCCAQoCggEBALEpj5ULY2MTP0CYbASap9g2LsK6HSkjjTXqhdo0
                                         SgWMMGaOUFxb5zDDE3GZR2OIqvJ7x94W7QHNkjArbevCt537Wb56oKKf3cXb558z
                                         csZXwJ+3yVqAoEVLXP61+usrQY+3C9UovxqyUKAoEszHqiHU7PXzw4iypCcIICLY
                                         +CqXFTVlLBZ7ChYJUcZwTIiione1+3yo2MzWnx9cYskQlhbDTzDg+gWe0vT4pPdT
                                         zJeXXBYVqDG445dHQfPNrwgUpYTYansmxbU9DyU2/J5XvI2PtZHQqlrMHXIBP0Ho
                                         +JXN/Z6WZlS7p6E97ElmNhUlwJOM+md+ehIll9mbzuMscjMCAwEAAaN8MHowHQYD
                                         VR0OBBYEFMtkqiGIhyWqefV67dITn3UeOAdPMB8GA1UdIwQYMBaAFMtkqiGIhyWq
                                         efV67dITn3UeOAdPMA8GA1UdEwEB/wQFMAMBAf8wJwYDVR0RBCAwHoILZXhhbXBs
                                         ZS5jb22CD3d3dy5leGFtcGxlLmNvbTANBgkqhkiG9w0BAQsFAAOCAQEAXDmgDXW1
                                         i+VePx/0/RtFlyr4YYy3y+8HvoN7SpD82ToBkeIttuJhLoHcrYqPb6iZx86Qbgb7
                                         Ly2hDbFDhfPcGKaK9n0zMc8vNvPd45iUMq0lZtLQxHPYkqabnCPFhnAfqux87k62
                                         LUm/QB2nFtf+1hEoKa5zyk3BgewbtA1sTHxemE8MuhWyv22aL3UKuD8iDLK/LXRk
                                         uGpsnfacD9+9GvOOUxh4WHnn6w/77H0Dx1O7+FA1ej3WNS7jnQUpVsNIqTo9hncr
                                         r1cvJAmeeNiflca7LsELDb1SaX1ehF2lEJYcOCGY5r6t9+8x8T8Bfml/1KPme93Y
                                         /y19yBxy7K0qyw==
                                         -----END CERTIFICATE-----
                                         """;

    private const string ValidKeyPem = """
                                        -----BEGIN PRIVATE KEY-----
                                        MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQCxKY+VC2NjEz9A
                                        mGwEmqfYNi7Cuh0pI4016oXaNEoFjDBmjlBcW+cwwxNxmUdjiKrye8feFu0BzZIw
                                        K23rwred+1m+eqCin93F2+efM3LGV8Cft8lagKBFS1z+tfrrK0GPtwvVKL8aslCg
                                        KBLMx6oh1Oz188OIsqQnCCAi2PgqlxU1ZSwWewoWCVHGcEyIoqJ3tft8qNjM1p8f
                                        XGLJEJYWw08w4PoFntL0+KT3U8yXl1wWFagxuOOXR0Hzza8IFKWE2Gp7JsW1PQ8l
                                        NvyeV7yNj7WR0KpazB1yAT9B6PiVzf2elmZUu6ehPexJZjYVJcCTjPpnfnoSJZfZ
                                        m87jLHIzAgMBAAECggEAQxxVUcaAnbVazqNut8fGMTdFO2q5RS48feIbVm9cYwGa
                                        DB94/aOqzmP3Z58C1gedikGtksnoejhfWnP5LcgTOntOocNeOnyIzDzjXwFkRxJS
                                        264JTolPLTDBR5O0O4WlTkWu686Fph1KQYEsrfoszqgUI4910MCrQkXntouuZqM3
                                        bLcbZYchTbZvybRbYftOi6xLRT3m8D8AGVN2zvG/+rFOq3G6yP7ipQAzDefsswTW
                                        5whAh8HcCxhiCUIbp9qTD3GyE0cz/t8zDL+J5HzZ6/OYbGWHvhg2XBiK6vmMKapX
                                        eFZNgalCRC8+945+AKeTFYGPwJJsCrgeVzy/L72jYQKBgQDb/7s5ZOUvh1CtslSi
                                        HIAqqkn/opS/rsCU0teaBHi7rmlDn3/BygDUsOIM9xjzTjbmKVg6VvEnLkYkrZMp
                                        D7aeHEtQjIhHGfwWJSqD+SDURoVLmkvCxqp3aCxNc3gxmQc8R8LpzjMvlbPqsij5
                                        Z3rO0ArojWahfFmWTc9tjuBU6wKBgQDOJ03stNFdXSpiiRVStP2RKMaC65NnxjBv
                                        T0QgyPWo/TL9o6XqD+tV0GbxR3QX7mP3qzUCSDmu3wyRwoErjpY3+gWWxufW39si
                                        aa0pzSJM2aiNJpUiRvAgdOEaHBqe/GEcWAZN7/Plzy3T5r+fo5ELPkX6+qKi2Vuj
                                        WCqa0pul2QKBgQC0DV+owIfGV2PTVQFpUBQhVw+LFf/RxW8+HjVwizpYuIzUWITS
                                        EMaPTFklrVIRRzEtPCdGUAO8QmYL/LdVQtP+IUAOo4WhU4X6hd5+9nVE5paPYq+g
                                        sMGxSmP/24JCbXD7h+vhOO6xgj8m1Tstq+BZxPE4lQmrHr+fgP1EOEwnkwKBgE3/
                                        mQAiOcS1Zz/41dSBHh856kHGl/L/jXvP5drxreDOS+ijbjbs5wGE5C4N9uLHE5O1
                                        d0zxvsFnKv5LNUwhmrx7IHo3r6gg8mxGx3m1X3DsOVWOb4aUiG3/StvyHjBhFO0A
                                        cQIz83fTt2chOwdPf6VdXmTjR32N95oJ1bTWUoWhAoGBAIcoDYWo1H0x4s6NcvVf
                                        dlmtl6M9MEZ9Ec4YqAeRGCMmgv4S4Cd0UjmArrPe7/eUIK4uYVpVlph8FteBBoym
                                        HELGZbSFHdeP3ZilE4spgf7ahl+v/OXTqMBeBL6jEO6L1IwXyh/qcZWTdcF6E34M
                                        H9TtMHRm1Gx5ENAnN6wa3Skx
                                        -----END PRIVATE KEY-----
                                        """;

    private IServiceRegistryEntryRepository _serviceRegistryEntryRepository;
    private ISslCertificateRepository _sslCertificateRepository;
    private ITraefikDynamicConfigWriter _traefikDynamicConfigWriter;
    private AttachDomainCertificateHandler _sut;

    [SetUp]
    public void Setup()
    {
        _serviceRegistryEntryRepository = Substitute.For<IServiceRegistryEntryRepository>();
        _sslCertificateRepository = Substitute.For<ISslCertificateRepository>();
        _traefikDynamicConfigWriter = Substitute.For<ITraefikDynamicConfigWriter>();
        _traefikDynamicConfigWriter.WriteDomainCertificateAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _sut = new AttachDomainCertificateHandler(
            _serviceRegistryEntryRepository, _sslCertificateRepository, _traefikDynamicConfigWriter);
    }

    [Test]
    public async Task Handle_EntryNotFound_ReturnsFailure()
    {
        var command = new AttachDomainCertificateCommand
        {
            DomainId = Guid.NewGuid(),
            CertificateId = Guid.NewGuid()
        };
        _serviceRegistryEntryRepository.GetByDomainIdAsync(command.DomainId, Arg.Any<CancellationToken>())
            .Returns((ServiceRegistryEntry?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_DomainNotCustomMode_ReturnsFailure()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("example.com", 80, TlsMode.Acme);
        var command = new AttachDomainCertificateCommand
        {
            DomainId = domain.Id,
            CertificateId = Guid.NewGuid()
        };
        _serviceRegistryEntryRepository.GetByDomainIdAsync(domain.Id, Arg.Any<CancellationToken>())
            .Returns(entry);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("INVALID_OPERATION");
    }

    [Test]
    public async Task Handle_CertificateNotFound_ReturnsFailure()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("example.com", 80, TlsMode.Custom);
        var command = new AttachDomainCertificateCommand
        {
            DomainId = domain.Id,
            CertificateId = Guid.NewGuid()
        };
        _serviceRegistryEntryRepository.GetByDomainIdAsync(domain.Id, Arg.Any<CancellationToken>())
            .Returns(entry);
        _sslCertificateRepository.GetByIdAsync(command.CertificateId, Arg.Any<CancellationToken>())
            .Returns((SslCertificate?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ValidAttach_SetsCertificateAndWritesDynamicConfig()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("other.example.com", 80, TlsMode.Custom);
        var certificate = SslCertificate.Create("wildcard", ValidCertPem, ValidKeyPem);
        var command = new AttachDomainCertificateCommand
        {
            DomainId = domain.Id,
            CertificateId = certificate.Id
        };
        _serviceRegistryEntryRepository.GetByDomainIdAsync(domain.Id, Arg.Any<CancellationToken>())
            .Returns(entry);
        _sslCertificateRepository.GetByIdAsync(certificate.Id, Arg.Any<CancellationToken>())
            .Returns(certificate);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Warnings.ShouldContain(w => w.Contains("do not include"));
        domain.SslCertificateId.ShouldBe(certificate.Id);
        await _traefikDynamicConfigWriter.Received(1).WriteDomainCertificateAsync(
            domain.Id, ValidCertPem, ValidKeyPem, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_SidecarOwnedDomain_SetsCertificateAndWritesDynamicConfig()
    {
        var entry = ServiceRegistryEntry.CreateForSidecar(Guid.NewGuid());
        var domain = entry.AddDomain("dashboard.example.com", 80, TlsMode.Custom);
        var certificate = SslCertificate.Create("wildcard", ValidCertPem, ValidKeyPem);
        var command = new AttachDomainCertificateCommand
        {
            DomainId = domain.Id,
            CertificateId = certificate.Id
        };
        _serviceRegistryEntryRepository.GetByDomainIdAsync(domain.Id, Arg.Any<CancellationToken>())
            .Returns(entry);
        _sslCertificateRepository.GetByIdAsync(certificate.Id, Arg.Any<CancellationToken>())
            .Returns(certificate);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        domain.SslCertificateId.ShouldBe(certificate.Id);
    }
}