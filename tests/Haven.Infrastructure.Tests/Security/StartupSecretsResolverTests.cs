using Haven.Infrastructure.Security;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Haven.Infrastructure.Tests.Security;

[Category("Unit")]
public sealed class StartupSecretsResolverTests
{
    private string _dir = null!;
    private string SecretsPath => Path.Combine(_dir, "secrets.json");

    [SetUp]
    public void Setup() => _dir = Directory.CreateTempSubdirectory("haven-secrets-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    private IConfiguration Config(string? key = null, string? jwt = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Encryption:Key"] = key,
            ["Jwt:Secret"] = jwt,
            [StartupSecretsResolver.SecretsPathKey] = SecretsPath
        }).Build();

    private static readonly string RealKey = Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());
    private const string RealJwt = "a-real-secret-that-is-longer-than-32-characters";

    private ResolvedSecrets Resolve(IConfiguration c, bool allowPlaceholders = false, bool ephemeral = false, bool existingInstall = false) =>
        StartupSecretsResolver.Resolve(c, allowPlaceholders, ephemeral, () => existingInstall);

    [Test]
    public void Resolve_ShouldUseConfiguredValues_WhenBothValid()
    {
        var result = Resolve(Config(RealKey, RealJwt));

        result.EncryptionKey.ShouldBe(RealKey);
        result.JwtSecret.ShouldBe(RealJwt);
        File.Exists(SecretsPath).ShouldBeFalse();
    }

    [Test]
    public void Resolve_ShouldGenerateAndPersist_WhenNothingConfiguredOnFreshInstall()
    {
        var result = Resolve(Config());

        SecretPolicy.ValidateEncryptionKeyStructure(result.EncryptionKey).ShouldBeNull();
        SecretPolicy.IsPlaceholderEncryptionKey(result.EncryptionKey).ShouldBeFalse();
        SecretPolicy.ValidateJwtSecretStructure(result.JwtSecret).ShouldBeNull();
        File.Exists(SecretsPath).ShouldBeTrue();
    }

    [Test]
    public void Resolve_ShouldReusePersistedSecrets_OnSubsequentStarts()
    {
        var first = Resolve(Config());
        var second = StartupSecretsResolver.Resolve(Config(), false, false, () => throw new InvalidOperationException("must not probe"));

        second.EncryptionKey.ShouldBe(first.EncryptionKey);
        second.JwtSecret.ShouldBe(first.JwtSecret);
    }

    [Test]
    public void Resolve_ShouldFallBackToLegacyKey_WhenExistingInstallAndNothingConfigured()
    {
        var result = Resolve(Config(), existingInstall: true);

        result.EncryptionKey.ShouldBe(SecretPolicy.LegacyEncryptionKey);
        result.Warnings.ShouldContain(w => w.Contains("INSECURE"));
        // The JWT secret has no legacy data to protect, so it is still generated.
        SecretPolicy.IsPlaceholderJwtSecret(result.JwtSecret).ShouldBeFalse();
    }

    [Test]
    public void Resolve_ShouldKeepUsingLegacyKeyAcrossRestarts_OfExistingInstall()
    {
        Resolve(Config(), existingInstall: true);
        var second = Resolve(Config(), existingInstall: true);

        second.EncryptionKey.ShouldBe(SecretPolicy.LegacyEncryptionKey);
    }

    [Test]
    public void Resolve_ShouldRejectPlaceholderKey_OutsideDevelopment()
    {
        Should.Throw<SecretsConfigurationException>(() => Resolve(Config(SecretPolicy.LegacyEncryptionKey, RealJwt)));
    }

    [Test]
    public void Resolve_ShouldRejectPlaceholderJwt_OutsideDevelopment()
    {
        Should.Throw<SecretsConfigurationException>(() => Resolve(Config(RealKey, SecretPolicy.LegacyJwtSecret)));
    }

    [Test]
    public void Resolve_ShouldAcceptPlaceholders_InDevelopmentWithWarning()
    {
        var result = Resolve(Config(SecretPolicy.LegacyEncryptionKey, SecretPolicy.LegacyJwtSecret), allowPlaceholders: true);

        result.Warnings.Count.ShouldBe(2);
    }

    [Test]
    public void Resolve_ShouldRejectMalformedKey_EvenInDevelopment()
    {
        Should.Throw<SecretsConfigurationException>(() => Resolve(Config("not-base64!!", RealJwt), allowPlaceholders: true));
        Should.Throw<SecretsConfigurationException>(() => Resolve(Config(Convert.ToBase64String(new byte[16]), RealJwt), allowPlaceholders: true));
    }

    [Test]
    public void Resolve_ShouldRejectShortJwtSecret()
    {
        Should.Throw<SecretsConfigurationException>(() => Resolve(Config(RealKey, "short"), allowPlaceholders: true));
    }

    [Test]
    public void Resolve_ShouldRejectPlaceholderInPersistedFile_EvenInDevelopment()
    {
        File.WriteAllText(SecretsPath, $$"""{"EncryptionKey":"{{SecretPolicy.LegacyEncryptionKey}}"}""");

        Should.Throw<SecretsConfigurationException>(() => Resolve(Config(), allowPlaceholders: true));
    }

    [Test]
    public void Resolve_ShouldNotTouchDiskOrProbe_WhenEphemeral()
    {
        var result = StartupSecretsResolver.Resolve(Config(), false, true, () => throw new InvalidOperationException("must not probe"));

        result.EncryptionKey.ShouldNotBeNullOrEmpty();
        File.Exists(SecretsPath).ShouldBeFalse();
    }

    [Test]
    public void Resolve_ShouldFail_WhenSecretsFileIsCorrupt()
    {
        File.WriteAllText(SecretsPath, "{ not json");

        Should.Throw<SecretsConfigurationException>(() => Resolve(Config()));
    }

    [Test]
    public void Resolve_ShouldRestrictPersistedFilePermissions_OnUnix()
    {
        Resolve(Config());

        if (!OperatingSystem.IsWindows())
            File.GetUnixFileMode(SecretsPath).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [Test]
    public void Validators_ShouldRejectStructurallyInvalidOptions()
    {
        new EncryptionOptionsValidator().Validate(null, new EncryptionOptions { Key = "" }).Failed.ShouldBeTrue();
        new EncryptionOptionsValidator().Validate(null, new EncryptionOptions { Key = RealKey }).Succeeded.ShouldBeTrue();
        new JwtOptionsValidator().Validate(null, new JwtOptions { Secret = "short" }).Failed.ShouldBeTrue();
        new JwtOptionsValidator().Validate(null, new JwtOptions { Secret = RealJwt }).Succeeded.ShouldBeTrue();
    }
}