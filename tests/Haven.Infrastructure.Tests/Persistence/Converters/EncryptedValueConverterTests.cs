using Haven.Application.Common.Interfaces;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Persistence.Converters;
using NSubstitute;
using Shouldly;

namespace Haven.Infrastructure.Tests.Persistence.Converters;

[Category("Unit")]
public sealed class EncryptedValueConverterTests
{
    private IEncryptionService _encryptionService = null!;
    private EncryptedValueConverter _sut = null!;

    [SetUp]
    public void Setup()
    {
        _encryptionService = Substitute.For<IEncryptionService>();
        _sut = new EncryptedValueConverter(_encryptionService);
    }

    [Test]
    public void ConvertToProvider_ShouldEncryptValue()
    {
        _encryptionService.Encrypt("plaintext").Returns("ciphertext");
        var value = EncryptedValue.From("plaintext");

        var result = _sut.ConvertToProviderExpression.Compile()(value);

        result.ShouldBe("ciphertext");
        _encryptionService.Received(1).Encrypt("plaintext");
    }

    [Test]
    public void ConvertFromProvider_ShouldDecryptValue()
    {
        _encryptionService.Decrypt("ciphertext").Returns("plaintext");

        var result = _sut.ConvertFromProviderExpression.Compile()("ciphertext");

        result.Value.ShouldBe("plaintext");
        _encryptionService.Received(1).Decrypt("ciphertext");
    }

    [Test]
    public void RoundTrip_ShouldReturnOriginalValue()
    {
        _encryptionService.Encrypt("secret").Returns("encrypted");
        _encryptionService.Decrypt("encrypted").Returns("secret");

        var original = EncryptedValue.From("secret");
        var stored = _sut.ConvertToProviderExpression.Compile()(original);
        var restored = _sut.ConvertFromProviderExpression.Compile()(stored);

        restored.ShouldBe(original);
    }
}
