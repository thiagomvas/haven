using System.Text.Json;

using Microsoft.Extensions.Configuration;

namespace Haven.Infrastructure.Security;

public sealed record ResolvedSecrets(string EncryptionKey, string JwtSecret, IReadOnlyList<string> Warnings);

public sealed class SecretsConfigurationException(string message) : InvalidOperationException(message);

/// <summary>
/// Decides which encryption key and JWT signing secret the process runs with, before the host is
/// built. Precedence: explicit configuration (env vars / appsettings) → secrets file persisted on the
/// data volume → freshly generated (and persisted). Placeholder values are rejected outside Development.
/// </summary>
public static class StartupSecretsResolver
{
    public const string SecretsPathKey = "Security:SecretsPath";
    public const string DefaultSecretsPath = "/data/secrets.json";

    private const string HowToFix =
        "Set Encryption__Key (base64, 32 random bytes, e.g. `openssl rand -base64 32`) and " +
        "Jwt__Secret (>= 32 random characters, e.g. `openssl rand -base64 64`) via environment variables, " +
        "or remove them so Haven generates and persists its own.";

    private sealed class SecretsFile
    {
        public string? EncryptionKey { get; set; }
        public string? JwtSecret { get; set; }
    }

    /// <param name="configuration">Application configuration (env vars, appsettings).</param>
    /// <param name="allowPlaceholders">True in Development: placeholder values only produce a warning.</param>
    /// <param name="ephemeral">True in tests: generate in-memory secrets, never touch disk or the database.</param>
    /// <param name="isExistingInstall">
    /// Lazily probes whether this is an install that predates generated secrets (its data was encrypted
    /// with the old zero key). Only invoked when no encryption key is configured or persisted.
    /// </param>
    public static ResolvedSecrets Resolve(
        IConfiguration configuration,
        bool allowPlaceholders,
        bool ephemeral,
        Func<bool> isExistingInstall)
    {
        var warnings = new List<string>();

        var configuredKey = Normalize(configuration[$"{EncryptionOptions.SectionName}:Key"]);
        var configuredJwt = Normalize(configuration[$"{JwtOptions.SectionName}:Secret"]);

        if (configuredKey is not null)
            ValidateConfiguredKey(configuredKey, allowPlaceholders, warnings);
        if (configuredJwt is not null)
            ValidateConfiguredJwt(configuredJwt, allowPlaceholders, warnings);

        if (configuredKey is not null && configuredJwt is not null)
            return new ResolvedSecrets(configuredKey, configuredJwt, warnings);

        if (ephemeral)
        {
            return new ResolvedSecrets(
                configuredKey ?? SecretPolicy.GenerateEncryptionKey(),
                configuredJwt ?? SecretPolicy.GenerateJwtSecret(),
                warnings);
        }

        var path = Path.GetFullPath(Normalize(configuration[SecretsPathKey]) ?? DefaultSecretsPath);
        var file = ReadFile(path);
        var dirty = false;

        var key = configuredKey ?? Normalize(file.EncryptionKey);
        if (configuredKey is null && key is not null)
        {
            ValidateConfiguredKey(key, allowPlaceholders: false, warnings, $"secrets file '{path}'");
        }
        else if (key is null)
        {
            if (isExistingInstall())
            {
                key = SecretPolicy.LegacyEncryptionKey;
                warnings.Add(
                    "No encryption key is configured, but this database already holds data encrypted with the old " +
                    "built-in default key. Falling back to that publicly known key so existing secrets keep working. " +
                    "This is INSECURE: anyone can decrypt stored secrets. " + HowToFix);
            }
            else
            {
                key = SecretPolicy.GenerateEncryptionKey();
                file.EncryptionKey = key;
                dirty = true;
                warnings.Add($"Generated a new encryption key and persisted it to '{path}'. Back this file up: without it, stored secrets cannot be decrypted.");
            }
        }

        var jwt = configuredJwt ?? Normalize(file.JwtSecret);
        if (configuredJwt is null && jwt is not null)
        {
            ValidateConfiguredJwt(jwt, allowPlaceholders: false, warnings, $"secrets file '{path}'");
        }
        else if (jwt is null)
        {
            jwt = SecretPolicy.GenerateJwtSecret();
            file.JwtSecret = jwt;
            dirty = true;
            warnings.Add($"Generated a new JWT signing secret and persisted it to '{path}'.");
        }

        if (dirty)
            WriteFile(path, file);

        return new ResolvedSecrets(key, jwt, warnings);
    }

    private static void ValidateConfiguredKey(string key, bool allowPlaceholders, List<string> warnings, string source = "configuration")
    {
        var structural = SecretPolicy.ValidateEncryptionKeyStructure(key);
        if (structural is not null)
            throw new SecretsConfigurationException($"Invalid Encryption:Key from {source}: {structural} {HowToFix}");

        if (!SecretPolicy.IsPlaceholderEncryptionKey(key))
            return;

        if (!allowPlaceholders)
            throw new SecretsConfigurationException(
                $"Encryption:Key from {source} is a known placeholder/all-zero key and is not allowed outside Development. " +
                "Existing data encrypted with it must be re-entered after switching to a real key. " + HowToFix);

        warnings.Add($"Encryption:Key from {source} is a placeholder. Acceptable in Development only.");
    }

    private static void ValidateConfiguredJwt(string secret, bool allowPlaceholders, List<string> warnings, string source = "configuration")
    {
        var structural = SecretPolicy.ValidateJwtSecretStructure(secret);
        if (structural is not null)
            throw new SecretsConfigurationException($"Invalid Jwt:Secret from {source}: {structural} {HowToFix}");

        if (!SecretPolicy.IsPlaceholderJwtSecret(secret))
            return;

        if (!allowPlaceholders)
            throw new SecretsConfigurationException(
                $"Jwt:Secret from {source} is a placeholder value and is not allowed outside Development; anyone could forge tokens with it. " + HowToFix);

        warnings.Add($"Jwt:Secret from {source} is a placeholder. Acceptable in Development only.");
    }

    private static SecretsFile ReadFile(string path)
    {
        if (!File.Exists(path))
            return new SecretsFile();

        try
        {
            return JsonSerializer.Deserialize<SecretsFile>(File.ReadAllText(path)) ?? new SecretsFile();
        }
        catch (JsonException ex)
        {
            throw new SecretsConfigurationException($"Secrets file '{path}' is not valid JSON ({ex.Message}). Fix or delete it. Deleting it loses the ability to decrypt stored secrets.");
        }
    }

    private static void WriteFile(string path, SecretsFile file)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true }));
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(temp, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new SecretsConfigurationException(
                $"Could not persist generated secrets to '{path}': {ex.Message}. " +
                $"Mount a writable volume there (see '{SecretsPathKey}') or supply the secrets explicitly. " + HowToFix);
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
