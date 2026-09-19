using System.Text.Json;

using Haven.Application.Features.HealthChecks;
using Haven.Domain.Enums;

using Shouldly;

namespace Haven.Application.Tests.Features.HealthChecks;

[Category("Unit")]
public sealed class HealthCheckConfigValidatorTests
{
    [TestCase(HealthCheckKind.Container, "", true)]
    [TestCase(HealthCheckKind.Http, """{"url":"http://{{container}}:8080/health","mode":"Probe"}""", true)]
    [TestCase(HealthCheckKind.Http, """{"url":"http://x","mode":"Direct"}""", true)]
    [TestCase(HealthCheckKind.Http, """{"url":""}""", false)]
    [TestCase(HealthCheckKind.Http, "", false)]
    [TestCase(HealthCheckKind.Bash, """{"command":"true"}""", true)]
    [TestCase(HealthCheckKind.Bash, """{"command":" "}""", false)]
    [TestCase(HealthCheckKind.Tcp, """{"host":"{{container}}","port":5432}""", true)]
    [TestCase(HealthCheckKind.Tcp, """{"host":"db","port":0}""", false)]
    [TestCase(HealthCheckKind.Tcp, """{"host":"db","port":70000}""", false)]
    [TestCase(HealthCheckKind.Tcp, """{"host":"","port":80}""", false)]
    public void IsValid_ValidatesPerKind(HealthCheckKind kind, string config, bool expected)
    {
        HealthCheckConfigValidator.IsValid(kind, config).ShouldBe(expected);
    }

    [Test]
    public void HttpConfig_DefaultsToDirectModeSoExistingChecksKeepWorking()
    {
        var config = JsonSerializer.Deserialize<HttpHealthCheckConfig>(
            """{"url":"http://localhost:8080/health"}""", HealthCheckConfigValidator.JsonOptions);

        config!.Mode.ShouldBe(HttpHealthCheckMode.Direct);
    }

    [Test]
    public void HttpConfig_ParsesProbeModeFromString()
    {
        var config = JsonSerializer.Deserialize<HttpHealthCheckConfig>(
            """{"url":"http://x","mode":"Probe"}""", HealthCheckConfigValidator.JsonOptions);

        config!.Mode.ShouldBe(HttpHealthCheckMode.Probe);
    }
}
