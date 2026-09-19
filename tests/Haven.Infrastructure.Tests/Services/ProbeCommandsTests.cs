using Haven.Application.Common.Interfaces.Services;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Infrastructure.Services;

using Shouldly;

namespace Haven.Infrastructure.Tests.Services;

[Category("Unit")]
public sealed class ProbeCommandsTests
{
    private static string CurlJson(int responseCode, double timeTotal = 0.0123) =>
        $$"""{"response_code":{{responseCode}},"time_total":{{timeTotal.ToString(System.Globalization.CultureInfo.InvariantCulture)}},"exitcode":0}""";

    private static ProbeOutput Http(string body, int responseCode, long exitCode = 0, string stderr = "", double timeTotal = 0.0123) =>
        new(exitCode, body + ProbeCommands.BodySeparator + CurlJson(responseCode, timeTotal), stderr, 500);

    [Test]
    public void ParseHttp_ExpectedStatus_IsHealthyWithTimingFromCurl()
    {
        var result = ProbeCommands.ParseHttp(Http("ok", 200), [200], "http://svc/health");

        result.Status.ShouldBe(ServiceHealth.Healthy);
        result.HttpStatusCode.ShouldBe(200);
        result.DurationMs.ShouldBe(12);
        result.Output.ShouldBe("ok");
    }

    [Test]
    public void ParseHttp_UnexpectedStatus_IsUnhealthyWithBodyAndReason()
    {
        var result = ProbeCommands.ParseHttp(Http("db down", 503), [200], "http://svc/health");

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.UnexpectedStatusCode);
        result.HttpStatusCode.ShouldBe(503);
        result.Message.ShouldContain("503");
        result.Output.ShouldBe("db down");
    }

    [Test]
    public void ParseHttp_EmptyExpectedCodes_DefaultsTo200()
    {
        ProbeCommands.ParseHttp(Http("", 200), [], "u").Status.ShouldBe(ServiceHealth.Healthy);
        ProbeCommands.ParseHttp(Http("", 204), [], "u").Status.ShouldBe(ServiceHealth.Unhealthy);
    }

    [Test]
    public void ParseHttp_BodyTooLarge_StillJudgesStatusCode()
    {
        var result = ProbeCommands.ParseHttp(Http("partial", 200, exitCode: 63, stderr: "maximum file size exceeded"), [200], "u");

        result.Status.ShouldBe(ServiceHealth.Healthy);
    }

    [TestCase(6, HealthCheckFailureReason.DnsFailure)]
    [TestCase(7, HealthCheckFailureReason.ConnectionRefused)]
    [TestCase(28, HealthCheckFailureReason.Timeout)]
    [TestCase(35, HealthCheckFailureReason.TlsError)]
    [TestCase(60, HealthCheckFailureReason.TlsError)]
    [TestCase(52, HealthCheckFailureReason.Error)]
    public void ParseHttp_CurlFailure_MapsExitCodeToReason(int exitCode, HealthCheckFailureReason expected)
    {
        var output = new ProbeOutput(exitCode, ProbeCommands.BodySeparator + CurlJson(0), "curl: (x) something happened", 100);

        var result = ProbeCommands.ParseHttp(output, [200], "http://svc");

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(expected);
        result.Output.ShouldBe("curl: (x) something happened");
        result.ExitCode.ShouldBe(exitCode);
    }

    [Test]
    public void ParseHttp_NoJsonBlock_IsTreatedAsFailure()
    {
        var result = ProbeCommands.ParseHttp(new ProbeOutput(7, "", "curl: (7) refused", 10), [200], "http://svc");

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.ConnectionRefused);
    }

    [Test]
    public void BuildHttpArgs_UsesMethodTimeoutAndEndOfOptionsMarker()
    {
        var args = ProbeCommands.BuildHttpArgs("HEAD", "http://svc:8080/h", 7);

        args.ShouldContain("HEAD");
        args[args.ToList().IndexOf("--max-time") + 1].ShouldBe("7");
        args[^2].ShouldBe("--");
        args[^1].ShouldBe("http://svc:8080/h");
    }

    [Test]
    public void ParseTcp_ExitZero_IsHealthy()
    {
        var result = ProbeCommands.ParseTcp(new ProbeOutput(0, "", "", 15), "svc", 5432, 5);

        result.Status.ShouldBe(ServiceHealth.Healthy);
    }

    [Test]
    public void ParseTcp_BadAddress_IsDnsFailure()
    {
        var result = ProbeCommands.ParseTcp(new ProbeOutput(1, "", "nc: bad address 'svc'", 20), "svc", 5432, 5);

        result.Reason.ShouldBe(HealthCheckFailureReason.DnsFailure);
    }

    [Test]
    public void ParseTcp_FailureAfterFullTimeout_IsTimeout()
    {
        var result = ProbeCommands.ParseTcp(new ProbeOutput(1, "", "", 5_010), "svc", 5432, 5);

        result.Reason.ShouldBe(HealthCheckFailureReason.Timeout);
    }

    [Test]
    public void ParseTcp_QuickFailure_IsConnectionRefused()
    {
        var result = ProbeCommands.ParseTcp(new ProbeOutput(1, "", "", 30), "svc", 5432, 5);

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.ConnectionRefused);
    }

    [Test]
    public void BuildTcpArgs_UsesZeroIoModeWithTimeout()
    {
        ProbeCommands.BuildTcpArgs("svc", 5432, 3).ShouldBe(["-z", "-w", "3", "svc", "5432"]);
    }
}
