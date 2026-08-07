using System.Text.Json;

using Haven.Application.Common.Contracts.Notifications;
using Haven.Application.Common.Interfaces;

namespace Haven.Application.Features.NotificationChannels;

/// <summary>
/// Encrypts/masks the password embedded inside a serialized <see cref="SmtpNotificationConfig"/>
/// (the config blob stored on <c>NotificationChannelConfig.Config</c>). Encrypted values are
/// prefixed with <see cref="EncryptedMarker"/> so a value can always be told apart from a
/// still-plaintext one without guessing at ciphertext shape — used both here and by the
/// one-time startup migration that re-encrypts pre-existing plaintext SMTP passwords.
/// </summary>
public static class SmtpConfigJsonCodec
{
    public const string EncryptedMarker = "enc:v1:";
    public const string MaskedPassword = "********";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static bool IsEncrypted(string? password) =>
        !string.IsNullOrEmpty(password) && password.StartsWith(EncryptedMarker, StringComparison.Ordinal);

    /// <summary>Encrypts the plaintext password in <paramref name="configJson"/>, if not already encrypted.</summary>
    public static string Encrypt(string configJson, IEncryptionService encryptionService)
    {
        var config = Deserialize(configJson);
        if (!string.IsNullOrEmpty(config.Password) && !IsEncrypted(config.Password))
            config.Password = EncryptedMarker + encryptionService.Encrypt(config.Password);

        return JsonSerializer.Serialize(config, SerializerOptions);
    }

    /// <summary>Replaces the password with a placeholder so it never round-trips to a client.</summary>
    public static string Mask(string configJson)
    {
        var config = Deserialize(configJson);
        config.Password = MaskedPassword;
        return JsonSerializer.Serialize(config, SerializerOptions);
    }

    /// <summary>
    /// Resolves the password for an update: if the incoming config's password is blank or still
    /// the mask placeholder, keeps the existing stored (encrypted) password instead of overwriting
    /// it; otherwise encrypts the newly-supplied plaintext password.
    /// </summary>
    public static string MergePasswordOnUpdate(string newConfigJson, string existingConfigJson, IEncryptionService encryptionService)
    {
        var newConfig = Deserialize(newConfigJson);
        if (string.IsNullOrEmpty(newConfig.Password) || newConfig.Password == MaskedPassword)
        {
            newConfig.Password = Deserialize(existingConfigJson).Password;
        }
        else if (!IsEncrypted(newConfig.Password))
        {
            newConfig.Password = EncryptedMarker + encryptionService.Encrypt(newConfig.Password);
        }

        return JsonSerializer.Serialize(newConfig, SerializerOptions);
    }

    /// <summary>Decrypts the password for send-time use. Returns the plaintext password.</summary>
    public static string DecryptPassword(string configJson, IEncryptionService encryptionService)
    {
        var config = Deserialize(configJson);
        return IsEncrypted(config.Password)
            ? encryptionService.Decrypt(config.Password[EncryptedMarker.Length..])
            : config.Password;
    }

    private static SmtpNotificationConfig Deserialize(string configJson) =>
        JsonSerializer.Deserialize<SmtpNotificationConfig>(configJson, SerializerOptions)
        ?? throw new InvalidOperationException("SMTP config could not be deserialized.");
}