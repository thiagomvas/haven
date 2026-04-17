using Haven.Domain.ValueObjects;
using Shouldly;

namespace Haven.Application.Tests.Domain.ValueObjects;

[Category("Unit")]
public sealed class EncryptedValueTests
{
    [Test]
    public void From_ShouldCreateInstance_WithGivenValue()
    {
        var value = EncryptedValue.From("secret");

        value.Value.ShouldBe("secret");
    }

    [Test]
    public void From_ShouldThrow_WhenValueIsEmpty()
    {
        Should.Throw<ArgumentException>(() => EncryptedValue.From(string.Empty));
    }

    [Test]
    public void From_ShouldThrow_WhenValueIsNull()
    {
        Should.Throw<ArgumentException>(() => EncryptedValue.From(null!));
    }

    [Test]
    public void ImplicitConversion_FromString_ShouldCreateInstance()
    {
        EncryptedValue value = "secret";

        value.Value.ShouldBe("secret");
    }

    [Test]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        var value = EncryptedValue.From("secret");

        string result = value;

        result.ShouldBe("secret");
    }

    [Test]
    public void ToString_ShouldReturnValue()
    {
        var value = EncryptedValue.From("secret");

        value.ToString().ShouldBe("secret");
    }

    [Test]
    public void Equality_ShouldBeEqual_WhenValuesMatch()
    {
        var a = EncryptedValue.From("secret");
        var b = EncryptedValue.From("secret");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Test]
    public void Equality_ShouldNotBeEqual_WhenValuesDiffer()
    {
        var a = EncryptedValue.From("secret-a");
        var b = EncryptedValue.From("secret-b");

        a.ShouldNotBe(b);
        (a != b).ShouldBeTrue();
    }

    [Test]
    public void GetHashCode_ShouldBeEqual_WhenValuesMatch()
    {
        var a = EncryptedValue.From("secret");
        var b = EncryptedValue.From("secret");

        a.GetHashCode().ShouldBe(b.GetHashCode());
    }
}
