using Haven.Domain.Entities;
using Haven.Domain.Exceptions;

using Shouldly;

namespace Haven.Domain.Tests.Entities;

[TestFixture]
[Category("Unit")]
public sealed class DomainCertificateTests
{
    // Self-signed test certificate for CN=example.com, SAN=example.com,www.example.com, valid
    // 2026-08-24 through 2027-08-24 (365 days).
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

    [Test]
    public void Create_ValidPair_ParsesMetadata()
    {
        var certificate = DomainCertificate.Create(Guid.NewGuid(), ValidCertPem, ValidKeyPem);

        certificate.SubjectCommonName.ShouldBe("example.com");
        certificate.NotBefore.ShouldBeLessThan(DateTimeOffset.UtcNow);
        certificate.NotAfter.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddDays(300));
        certificate.Fingerprint.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Create_ValidPair_DoesNotThrowEvenThoughNotYetExpired()
    {
        Should.NotThrow(() => DomainCertificate.Create(Guid.NewGuid(), ValidCertPem, ValidKeyPem));
    }

    [Test]
    public void Create_EmptyCertificatePem_Throws()
    {
        Should.Throw<ValidationException>(() => DomainCertificate.Create(Guid.NewGuid(), "", ValidKeyPem));
    }

    [Test]
    public void Create_EmptyPrivateKeyPem_Throws()
    {
        Should.Throw<ValidationException>(() => DomainCertificate.Create(Guid.NewGuid(), ValidCertPem, ""));
    }

    [Test]
    public void Create_UnparseablePem_Throws()
    {
        Should.Throw<ValidationException>(() =>
            DomainCertificate.Create(Guid.NewGuid(), "not a certificate", "not a key"));
    }

    [Test]
    public void Create_MismatchedKey_Throws()
    {
        const string otherKeyPem = """
                                    -----BEGIN PRIVATE KEY-----
                                    MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC7VJTUt9Us8cKj
                                    MzEfYyjiWA4R4/M2bS1GB4t7NXp98C3SC6dVMvDuictGeurT8jNbvJZHtCSuYEvu
                                    NMoSfm76oqFvAp8Gy0iz5sxjZmSnXyCdPEovGhLa0VzMaQ8s+CLOyS56YyCFGeJZ
                                    qgtzJ6GR3eqoYSW9b9UMvkBpZODSctWSNGj3P7jRFDO5VoTwCQAWbFnOjDfH5Ulg
                                    p2PKSQnSJP3AJLQNFNe7br1XbrhV//eO+t51mIpGSDCUv3E0DDFcWDTH9cXDTTlR
                                    ZVEiR2BwpZOOkE/Z0/BVnhZYL71oZV34bKfWjQIt6V/isSMahdsAASACp4ZTGtwi
                                    VuNd9tybAgMBAAECggEBAKTmjaS6tkK8BlPXClTQ2vpz/N6uxDeS35mXpqasqskV
                                    laAidgg/sWqpjXDbXr93otIMLlWsM+X0CqMDgSXKejLS2jx4GDjI1ZTXg++0AMJ8
                                    sJ74pWzVDOfmCEQ/7wXs3+cbnXhKriO8Z036q92Qc1+N87SI38nkGa0ABH9CN83H
                                    mQqt4fB7UdHzuIRe/me2PGhIq5ZBzj6h3BpoPGzEP+x3l9YmK8t/1cN0pqI+dQwY
                                    dgfGjackLu/2qRnninJ8gJKYDlJz9SIrf7BAf7B3Yhd8n5rgB8k+ZlBWNr71+f2r
                                    dUnGqxMY4iw7T2gGkAgIB1sMPzYo3JU1IK3PpLwYcuECgYEA8dQXaGRmtsgqLpV6
                                    E/tZRz1EDYW0aScilRAJIkG3MLKfhL3jhTLR7RXVJ4KAf2MvBORAxRJhOCqz2LTk
                                    l7KZKPXNAVDdWQeM70DLKdEEV3ZLGGjRTmqp7dxaZJqTa8gAYT8UBpqCS4orJ3d/
                                    ff8Btdk+kt4CGdfMBcqm0X4ATdECgYEAxevGWvVJqOoc+8LNwzWfMxdfr/8Iq5Ie
                                    ANsAeNKuNyJnYzoBGmqAWCPKuAt6MjLmfEXhP1sHXQnZE4LWfXwDNhZPZi7dXvV9
                                    HgS5xQAHt3ekasqmvz7oGaLU2WEuZDl15QeCiKGpUx5oPqCS9xQEfgGH2rmVzoGz
                                    Uo+9DUzOfUcCgYEAjq4jUUieRUuz4pWH4NBFsK+SjB2NRfhOhTaLM0AeqmSPPKgi
                                    xzR6HVYA0aY9AqPZeydtSYh/rNVeQR6yhKPnRElB5x7azQOoiI+wKfIRXWANz19N
                                    4o9KpiKcpqZ0LlOVwUuv/38xk9CedC9uCP4a5D69B0iWzGvhK8xdrM8XxvECgYAe
                                    +iqrbA9UT9DjHFDaP+jn+SF63pxgz53pAA0HXn/mnDBmpv2CClJ8FooZLuIQAo5Q
                                    aeafhKvduPRAKZ8IVwoRA4KDdInBWzCr6C1fBcNZ8yTXHNMKfz8H+m3AtNfyzTv1
                                    lWQ1nzyeaLDQ+PMDpqRIT1z0FhwZ2XwqA6bQeJgUqQKBgQDsMd3TIB6DK5jTVDZP
                                    /Ivg6uEEyq5ZOK7c5o7lYy9v/Aqk3/gcJ0FQeYs05L2FLE1qgh6XKgcwbYtI6vzl
                                    IiBH7Q26H6dGItAKcNGPWiWGVYK6MkQXwANI7Isq83NoyowaG8+MVw2K/rlF+FQq
                                    U8OY2AaeE+jjLKtdrKUxLLTwpQ==
                                    -----END PRIVATE KEY-----
                                    """;

        Should.Throw<ValidationException>(() =>
            DomainCertificate.Create(Guid.NewGuid(), ValidCertPem, otherKeyPem));
    }

    [Test]
    public void MatchesHostname_HostnameInSan_ReturnsTrue()
    {
        var certificate = DomainCertificate.Create(Guid.NewGuid(), ValidCertPem, ValidKeyPem);

        certificate.MatchesHostname("www.example.com").ShouldBeTrue();
    }

    [Test]
    public void MatchesHostname_HostnameNotCovered_ReturnsFalse()
    {
        var certificate = DomainCertificate.Create(Guid.NewGuid(), ValidCertPem, ValidKeyPem);

        certificate.MatchesHostname("other.example.com").ShouldBeFalse();
    }

    [Test]
    public void Rotate_ReplacesFieldsAndBumpsUpdatedAt()
    {
        var certificate = DomainCertificate.Create(Guid.NewGuid(), ValidCertPem, ValidKeyPem);
        var originalUpdatedAt = certificate.UpdatedAt;

        certificate.Rotate(ValidCertPem, ValidKeyPem);

        certificate.UpdatedAt.ShouldBeGreaterThanOrEqualTo(originalUpdatedAt);
        certificate.CertificatePem.ShouldBe(ValidCertPem);
    }

    [Test]
    public void IsExpired_FutureExpiry_ReturnsFalse()
    {
        var certificate = DomainCertificate.Create(Guid.NewGuid(), ValidCertPem, ValidKeyPem);

        certificate.IsExpired.ShouldBeFalse();
    }
}
