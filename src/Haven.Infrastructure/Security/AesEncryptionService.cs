using System.Security.Cryptography;
using System.Text;

using Haven.Application.Common.Interfaces;

using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.Security;

public sealed class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IOptions<EncryptionOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.Key);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption key must be 32 bytes (AES-256).");
    }

    public string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        // Layout: [16-byte IV][ciphertext]
        var blob = new byte[aes.IV.Length + ciphertextBytes.Length];
        aes.IV.CopyTo(blob, 0);
        ciphertextBytes.CopyTo(blob, aes.IV.Length);

        return Convert.ToBase64String(blob);
    }

    public string Decrypt(string ciphertext)
    {
        var blob = Convert.FromBase64String(ciphertext);

        using var aes = Aes.Create();
        aes.Key = _key;

        var ivLength = aes.BlockSize / 8;
        aes.IV = blob[..ivLength];

        using var decryptor = aes.CreateDecryptor();
        var plaintextBytes = decryptor.TransformFinalBlock(blob, ivLength, blob.Length - ivLength);

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}