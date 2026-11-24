using System.Security.Cryptography;

namespace Haven.Infrastructure.Security;

/// <summary>
/// Rules for what counts as an acceptable encryption key / JWT signing secret, including the
/// placeholder values that used to ship in the committed appsettings.json.
/// </summary>
public static class SecretPolicy
{
    public const int EncryptionKeyBytes = 32;
    public const int MinJwtSecretLength = 32;
    public const string PlaceholderJwtPrefix = "CHANGE_ME";

    /// <summary>The all-zero AES key (base64) that used to ship as the default in appsettings.json.</summary>
    public static readonly string LegacyEncryptionKey = Convert.ToBase64String(new byte[EncryptionKeyBytes]);

    /// <summary>The JWT secret that used to ship as the default in appsettings.json.</summary>
    public const string LegacyJwtSecret = "CHANGE_ME_use_a_32plus_char_secret_key_here!";

    public static string GenerateEncryptionKey() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(EncryptionKeyBytes));

    public static string GenerateJwtSecret() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    /// <summary>Returns an error message when the key is structurally unusable, otherwise null.</summary>
    public static string? ValidateEncryptionKeyStructure(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "Encryption key is empty.";

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(key);
        }
        catch (FormatException)
        {
            return "Encryption key is not valid base64.";
        }

        return bytes.Length == EncryptionKeyBytes
            ? null
            : $"Encryption key must decode to {EncryptionKeyBytes} bytes (AES-256), but decodes to {bytes.Length}.";
    }

    /// <summary>Returns an error message when the secret is structurally unusable, otherwise null.</summary>
    public static string? ValidateJwtSecretStructure(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return "JWT secret is empty.";

        return secret.Length >= MinJwtSecretLength
            ? null
            : $"JWT secret must be at least {MinJwtSecretLength} characters long.";
    }

    /// <summary>True for the known-public default key or any all-zero key.</summary>
    public static bool IsPlaceholderEncryptionKey(string key)
    {
        if (string.Equals(key, LegacyEncryptionKey, StringComparison.Ordinal))
            return true;

        try
        {
            return Convert.FromBase64String(key).All(b => b == 0);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>True for the known-public default secret or any value starting with the placeholder marker.</summary>
    public static bool IsPlaceholderJwtSecret(string secret) =>
        secret.StartsWith(PlaceholderJwtPrefix, StringComparison.OrdinalIgnoreCase);
}
