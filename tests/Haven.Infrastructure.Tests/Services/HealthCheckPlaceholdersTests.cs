using Haven.Infrastructure.Services;

using Shouldly;

namespace Haven.Infrastructure.Tests.Services;

[Category("Unit")]
public sealed class HealthCheckPlaceholdersTests
{
    private static readonly HealthCheckTarget Target = new("haven-proj-env-api", 8080, "haven-proj-env", true);

    [Test]
    public void Apply_ReplacesContainerAndPort()
    {
        var value = HealthCheckPlaceholders.Apply("http://{{container}}:{{port}}/health", Target, out var error);

        value.ShouldBe("http://haven-proj-env-api:8080/health");
        error.ShouldBeNull();
    }

    [Test]
    public void Apply_IsCaseInsensitive()
    {
        HealthCheckPlaceholders.Apply("http://{{Container}}/", Target, out _).ShouldBe("http://haven-proj-env-api/");
    }

    [Test]
    public void Apply_WithoutPlaceholders_ReturnsInputUnchanged()
    {
        HealthCheckPlaceholders.Apply("http://example.com/x", Target, out _).ShouldBe("http://example.com/x");
    }

    [Test]
    public void Apply_PortPlaceholderWithoutExposedPort_ReportsError()
    {
        var value = HealthCheckPlaceholders.Apply("http://{{container}}:{{port}}/", Target with { Port = null }, out var error);

        value.ShouldBeNull();
        error.ShouldContain("port");
    }

    [Test]
    public void Apply_UnknownPlaceholder_ReportsError()
    {
        var value = HealthCheckPlaceholders.Apply("http://{{host}}/", Target, out var error);

        value.ShouldBeNull();
        error.ShouldContain("unknown placeholder");
    }
}
