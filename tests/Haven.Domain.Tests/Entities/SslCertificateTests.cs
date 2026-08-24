using Haven.Domain.Entities;
using Haven.Domain.Exceptions;

using Shouldly;

namespace Haven.Domain.Tests.Entities;

[TestFixture]
[Category("Unit")]
public sealed class SslCertificateTests
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

    // Self-signed wildcard test certificate for CN=*.example.org, SAN=*.example.org.
    private const string WildcardCertPem = """
                                            -----BEGIN CERTIFICATE-----
                                            MIIDKzCCAhOgAwIBAgIUKpudQksbN5NyEMf6f4QWIMWKkMswDQYJKoZIhvcNAQEL
                                            BQAwGDEWMBQGA1UEAwwNKi5leGFtcGxlLm9yZzAeFw0yNjA4MjQxMjAxMjdaFw0z
                                            NjA4MjExMjAxMjdaMBgxFjAUBgNVBAMMDSouZXhhbXBsZS5vcmcwggEiMA0GCSqG
                                            SIb3DQEBAQUAA4IBDwAwggEKAoIBAQDcJ7vpCnt5ZmdWqWwnDaPGKh8m/O1zIaIu
                                            +O4DTi4n9LmbbjWVDZHatIwLAfpAGVRfJ/gPNn0H3sASmaY8ZTW26Zw8BeOsu+8R
                                            CoYQVZDErHzgjnwjMQgSZFGsnKQe2m716OWuaUSMsr86Y0jJBr388rZyz81qnYzM
                                            Yuj07OSV4D/8P7fSrnw+rKTnFD5ewnAxKy+qSEHTl0+le4VpT19Km9Eiu2SVjhrE
                                            6x3ZQx1+JDel08o1+YFJdHkrdAQL7fG9HkjJiq6FqvH9bzRhb/ZNL1tzPCugiJfR
                                            FH7aL1aDFVBHon3TrsubuQuJA4HUPdioPZADtDUGOA34elQZHiD9AgMBAAGjbTBr
                                            MB0GA1UdDgQWBBQxxPl5By8Ek0YEpOQfC/LfkLs4LzAfBgNVHSMEGDAWgBQxxPl5
                                            By8Ek0YEpOQfC/LfkLs4LzAPBgNVHRMBAf8EBTADAQH/MBgGA1UdEQQRMA+CDSou
                                            ZXhhbXBsZS5vcmcwDQYJKoZIhvcNAQELBQADggEBAB5hJCEAEtJ9bkW85I9vpIKm
                                            3nbD+USbKreffAYN2dAn24hvIOKG2fl9lsaShi+dVF6SoO7KiY0WMAXnptxOYjOl
                                            akM8FgOJ9w6RXSMxZUlSxaI8qlPlQ4cvOl+mEvgKQwsCXoJ4q5BLjEsTo0mOagPs
                                            v5ZteBl9/TujwcAAY+wRcKaEs/dPU5v/CBncCa62xOn4XgVPE9oRpbeP5apU3OYN
                                            jpgooJpSPO9QARQ+QAZcoma67m9krwLJdOLBa0M2Xwz4iSYvC5YzkZY8sTrELDld
                                            VOEgjVuGr+3f1io1PaLMQJR8GOv5+H6IKfZEwVGwGwpOqpswAL1xy7Qa4jlaZsA=
                                            -----END CERTIFICATE-----
                                            """;

    private const string WildcardKeyPem = """
                                           -----BEGIN PRIVATE KEY-----
                                           MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDcJ7vpCnt5ZmdW
                                           qWwnDaPGKh8m/O1zIaIu+O4DTi4n9LmbbjWVDZHatIwLAfpAGVRfJ/gPNn0H3sAS
                                           maY8ZTW26Zw8BeOsu+8RCoYQVZDErHzgjnwjMQgSZFGsnKQe2m716OWuaUSMsr86
                                           Y0jJBr388rZyz81qnYzMYuj07OSV4D/8P7fSrnw+rKTnFD5ewnAxKy+qSEHTl0+l
                                           e4VpT19Km9Eiu2SVjhrE6x3ZQx1+JDel08o1+YFJdHkrdAQL7fG9HkjJiq6FqvH9
                                           bzRhb/ZNL1tzPCugiJfRFH7aL1aDFVBHon3TrsubuQuJA4HUPdioPZADtDUGOA34
                                           elQZHiD9AgMBAAECggEAC+Rb5rucTgNi0iApOYEnsgDc+WzAq88q36mD0SsO3GAr
                                           CtMSHbjOV+IYTXuVcmhxMbRVlobINrMI14hesypOvjsSEij0R9nypjjrYyRSw1ju
                                           OZgLaxWPMiUinKBtJMXXqBIPnr33crfbaLKWatYh0EZ7u1RvwWenbx6UUa9JLVzx
                                           GEbTr9CHk+ydUsWvvlqGDhAPMING0PCEBh0nHZ0mWf0NOnIAEH+htVteeIvzt8Nc
                                           cF6pWYBiFJh/Zeos1dLa7UsT4cSDO/Ur3Ra2exNTWrUfeR6bk7BE+O340Y/LDNhH
                                           FVDU45SbzuE7Jj2xu5DMtKEKB+fDjBjL10pKgEJtaQKBgQDzBKJTVYAgp0AY+h3V
                                           nkVn7xjK3VgdOBtUpVspjzIoWCHYv63Acw09g6EXRcKgeRTWHlTrJ5P/KWjXU/wq
                                           GfuDkLIVcOHYglCk3j75zAL9vNGxMfoZAM/ViyRk096JVCX7asnN5FWpu1nGwe7z
                                           Fh/mCOubx7t7D0xdHuW1XNeH+QKBgQDn6nDtfCWiMSnFKjTSVyq28i+dYCMVPjFD
                                           k7U4VQ3R9KvEcOvSP13qUI9erfyY7gKVsZNSDISAUyqQtBIkCBXrM25KgqCzUPWR
                                           WZngJTgmcG20sus7kyTgMnP/bCgg4jWbxcQhCRME7WRYvptP1xflWIAV1BUQrg6O
                                           mL2q71XKJQKBgQCbzqLMTwsg5FpiKSorpZfWNSNuHU+7HBfZw1KZaKe92hOJRgt+
                                           UcVxZQ1JQH6yKC9FwJitU+i9Na10MPKBg7sP9RtYR9Fk4NgXfC5gNX7Nc9v1gZdZ
                                           pH2b6ePhiT0qSvs3IJZWHUkW03mRxxEOZWb6M0nrzLjVA0/wfDjGeMnu8QKBgBa5
                                           vHMpFS79jlBJwH9UF1VyCgRr5UQxofYzRTDN9Nq8FRDc1970YqmRV1s5xWTe/dXZ
                                           XsxNebZxb9xaKOTq/ercUVRv1Ht91XJ2y0NRolzx624nkjF2S8jEaOWAnbYLNKGd
                                           EYkDMJ/s+0ZO9z0toKPStkptS9skkzyZ7wwPA+MZAoGAfqQCizTMYhgZWEEvBXbo
                                           RtJLZ068gus1a9uFGXjbBrH1sPRawfXE2iO73O7UYjILKK8KR7H4A3XgC81nhHl9
                                           d4jNwLCrczSKX9fUML8XSkrASKsdosTq1LDjRhFsF/gmsDUQIsna68H5PfIyYJfG
                                           G+MS3nJR09/s0I2SIJCvxJ8=
                                           -----END PRIVATE KEY-----
                                           """;

    [Test]
    public void Create_ValidPair_ParsesMetadata()
    {
        var certificate = SslCertificate.Create("test", ValidCertPem, ValidKeyPem);

        certificate.SubjectCommonName.ShouldBe("example.com");
        certificate.NotBefore.ShouldBeLessThan(DateTimeOffset.UtcNow);
        certificate.NotAfter.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddDays(300));
        certificate.Fingerprint.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Create_ValidPair_DoesNotThrowEvenThoughNotYetExpired()
    {
        Should.NotThrow(() => SslCertificate.Create("test", ValidCertPem, ValidKeyPem));
    }

    [Test]
    public void Create_EmptyName_Throws()
    {
        Should.Throw<ValidationException>(() => SslCertificate.Create("", ValidCertPem, ValidKeyPem));
    }

    [Test]
    public void Create_EmptyCertificatePem_Throws()
    {
        Should.Throw<ValidationException>(() => SslCertificate.Create("test", "", ValidKeyPem));
    }

    [Test]
    public void Create_EmptyPrivateKeyPem_Throws()
    {
        Should.Throw<ValidationException>(() => SslCertificate.Create("test", ValidCertPem, ""));
    }

    [Test]
    public void Create_UnparseablePem_Throws()
    {
        Should.Throw<ValidationException>(() =>
            SslCertificate.Create("test", "not a certificate", "not a key"));
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
            SslCertificate.Create("test", ValidCertPem, otherKeyPem));
    }

    [Test]
    public void MatchesHostname_HostnameInSan_ReturnsTrue()
    {
        var certificate = SslCertificate.Create("test", ValidCertPem, ValidKeyPem);

        certificate.MatchesHostname("www.example.com").ShouldBeTrue();
    }

    [Test]
    public void MatchesHostname_HostnameNotCovered_ReturnsFalse()
    {
        var certificate = SslCertificate.Create("test", ValidCertPem, ValidKeyPem);

        certificate.MatchesHostname("other.example.com").ShouldBeFalse();
    }

    [Test]
    public void MatchesHostname_WildcardCoversSingleLevelSubdomain_ReturnsTrue()
    {
        var certificate = SslCertificate.Create("test", WildcardCertPem, WildcardKeyPem);

        certificate.MatchesHostname("app.example.org").ShouldBeTrue();
    }

    [Test]
    public void MatchesHostname_WildcardDoesNotCoverBareApex_ReturnsFalse()
    {
        var certificate = SslCertificate.Create("test", WildcardCertPem, WildcardKeyPem);

        certificate.MatchesHostname("example.org").ShouldBeFalse();
    }

    [Test]
    public void MatchesHostname_WildcardDoesNotCoverNestedSubdomain_ReturnsFalse()
    {
        var certificate = SslCertificate.Create("test", WildcardCertPem, WildcardKeyPem);

        certificate.MatchesHostname("a.b.example.org").ShouldBeFalse();
    }

    [Test]
    public void Rotate_ReplacesFieldsAndBumpsUpdatedAt()
    {
        var certificate = SslCertificate.Create("test", ValidCertPem, ValidKeyPem);
        var originalUpdatedAt = certificate.UpdatedAt;

        certificate.Rotate(ValidCertPem, ValidKeyPem);

        certificate.UpdatedAt.ShouldBeGreaterThanOrEqualTo(originalUpdatedAt);
        certificate.CertificatePem.ShouldBe(ValidCertPem);
    }

    [Test]
    public void IsExpired_FutureExpiry_ReturnsFalse()
    {
        var certificate = SslCertificate.Create("test", ValidCertPem, ValidKeyPem);

        certificate.IsExpired.ShouldBeFalse();
    }
}
