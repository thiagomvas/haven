using Haven.Infrastructure.Security;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Haven.Infrastructure.Tests.Security;

[Category("Unit")]
public sealed class AesEncryptionServiceTests
{
    private AesEncryptionService _sut = null!;

    // Valid 32-byte key, base64-encoded
    private const string ValidKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    [SetUp]
    public void Setup()
    {
        _sut = CreateService(ValidKey);
    }

    [Test]
    public void Constructor_ShouldThrow_WhenKeyIsNot32Bytes()
    {
        var shortKey = Convert.ToBase64String(new byte[16]);

        Should.Throw<InvalidOperationException>(() => CreateService(shortKey));
    }

    [Test]
    public void Encrypt_ShouldReturnBase64String()
    {
        var result = _sut.Encrypt("hello");

        Should.NotThrow(() => Convert.FromBase64String(result));
    }

    [Test]
    public void Encrypt_ShouldProduceDifferentCiphertext_ForSameInput()
    {
        var first = _sut.Encrypt("hello");
        var second = _sut.Encrypt("hello");

        first.ShouldNotBe(second);
    }

    [Test]
    public void Decrypt_ShouldReturnOriginalPlaintext()
    {
        var plaintext = "my secret value";
        var ciphertext = _sut.Encrypt(plaintext);

        var result = _sut.Decrypt(ciphertext);

        result.ShouldBe(plaintext);
    }

    [Test]
    [TestCase("simple")]
    [TestCase("unicode: 日本語")]
    [TestCase("special chars: !@#$%^&*()")]
    [TestCase("a very long value that exceeds a single AES block boundary by quite a margin")]
    public void RoundTrip_ShouldReturnOriginalValue(string plaintext)
    {
        var ciphertext = _sut.Encrypt(plaintext);
        var result = _sut.Decrypt(ciphertext);

        result.ShouldBe(plaintext);
    }

    [Test]
    public void Decrypt_ShouldThrow_WhenCiphertextIsTampered()
    {
        var ciphertext = _sut.Encrypt("hello");
        var blob = Convert.FromBase64String(ciphertext);
        blob[^1] ^= 0xFF;
        var tampered = Convert.ToBase64String(blob);

        Should.Throw<Exception>(() => _sut.Decrypt(tampered));
    }

    private static AesEncryptionService CreateService(string key) =>
        new(Options.Create(new EncryptionOptions { Key = key }));
}