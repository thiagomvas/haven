namespace Haven.Infrastructure.Security;

public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    /// <summary>Base64-encoded 32-byte AES-256 key.</summary>
    public string Key { get; set; } = string.Empty;
}
