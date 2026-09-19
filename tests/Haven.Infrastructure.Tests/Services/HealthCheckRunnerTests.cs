using System.Net;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Services;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Services;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Services;

[Category("Unit")]
public sealed class HealthCheckRunnerTests
{
    private static readonly Guid ServiceId = Guid.NewGuid();

    private IHealthCheckProbe _probe = null!;
    private IHealthCheckTargetResolver _resolver = null!;
    private FakeHandler _handler = null!;
    private HttpHealthCheckRunner _http = null!;
    private TcpHealthCheckRunner _tcp = null!;

    [SetUp]
    public void Setup()
    {
        _probe = Substitute.For<IHealthCheckProbe>();
        _resolver = Substitute.For<IHealthCheckTargetResolver>();
        _handler = new FakeHandler();

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(_handler, disposeHandler: false));

        _http = new HttpHealthCheckRunner(factory, _probe, _resolver, Substitute.For<ILogger<HttpHealthCheckRunner>>());
        _tcp = new TcpHealthCheckRunner(_probe, _resolver, Substitute.For<ILogger<TcpHealthCheckRunner>>());
    }

    [TearDown]
    public void TearDown() => _handler.Dispose();

    private static HealthCheck Http(string config) => HealthCheck.Create(ServiceId, "h", HealthCheckKind.Http, true, null, config);
    private static HealthCheck Tcp(string config) => HealthCheck.Create(ServiceId, "t", HealthCheckKind.Tcp, true, null, config);

    private void Target(HealthCheckTarget target) =>
        _resolver.ResolveAsync(ServiceId, Arg.Any<CancellationToken>()).Returns(Result<HealthCheckTarget>.Success(target));

    private static HealthCheckTarget Running(string? network = "haven-p-e", int? port = 8080) => new("haven-p-e-api", port, network, true);

    private void ProbeReturns(ProbeOutput output) =>
        _probe.RunAsync(Arg.Any<ProbeRequest>(), Arg.Any<CancellationToken>()).Returns(Result<ProbeOutput>.Success(output));

    // ---- HTTP: config -------------------------------------------------------------------------------------------

    [Test]
    public async Task Http_InvalidJson_IsUnknownInvalidConfig()
    {
        var result = await _http.RunHealthCheckAsync(Http("{not json"));

        result.Status.ShouldBe(ServiceHealth.Unknown);
        result.Reason.ShouldBe(HealthCheckFailureReason.InvalidConfig);
    }

    [Test]
    public async Task Http_EmptyUrl_IsUnknownInvalidConfig()
    {
        var result = await _http.RunHealthCheckAsync(Http("""{"url":""}"""));

        result.Status.ShouldBe(ServiceHealth.Unknown);
        result.Message.ShouldContain("URL");
    }

    // ---- HTTP: direct -------------------------------------------------------------------------------------------

    [Test]
    public async Task Http_Direct_ExpectedStatus_IsHealthyWithoutUsingProbe()
    {
        _handler.Respond(HttpStatusCode.OK);

        var result = await _http.RunHealthCheckAsync(Http("""{"url":"http://localhost/h"}"""));

        result.Status.ShouldBe(ServiceHealth.Healthy);
        result.HttpStatusCode.ShouldBe(200);
        await _probe.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
    }

    [Test]
    public async Task Http_Direct_UnexpectedStatus_ReportsCodeInReason()
    {
        _handler.Respond(HttpStatusCode.InternalServerError);

        var result = await _http.RunHealthCheckAsync(Http("""{"url":"http://localhost/h"}"""));

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.UnexpectedStatusCode);
        result.HttpStatusCode.ShouldBe(500);
    }

    [Test]
    public async Task Http_Direct_ConnectionError_IsUnhealthyConnectionRefused()
    {
        _handler.Throw(new HttpRequestException("refused", new System.Net.Sockets.SocketException(111), null));

        var result = await _http.RunHealthCheckAsync(Http("""{"url":"http://localhost/h"}"""));

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Message.ShouldContain("http://localhost/h");
    }

    [Test]
    public async Task Http_Direct_NameResolutionError_IsDnsFailure()
    {
        _handler.Throw(new HttpRequestException(HttpRequestError.NameResolutionError, "no such host"));

        var result = await _http.RunHealthCheckAsync(Http("""{"url":"http://nope/h"}"""));

        result.Reason.ShouldBe(HealthCheckFailureReason.DnsFailure);
    }

    [Test]
    public async Task Http_Direct_Placeholder_IsInvalidConfigWithHint()
    {
        var result = await _http.RunHealthCheckAsync(Http("""{"url":"http://{{container}}/h"}"""));

        result.Status.ShouldBe(ServiceHealth.Unknown);
        result.Reason.ShouldBe(HealthCheckFailureReason.InvalidConfig);
        result.Message.ShouldContain("Probe");
    }

    // ---- HTTP: probe --------------------------------------------------------------------------------------------

    [Test]
    public async Task Http_Probe_ResolvesPlaceholdersAndRunsCurlOnTargetNetwork()
    {
        Target(Running());
        ProbeReturns(new ProbeOutput(0, "ok" + ProbeCommands.BodySeparator + """{"response_code":200,"time_total":0.01}""", "", 300));

        var result = await _http.RunHealthCheckAsync(Http("""{"url":"http://{{container}}:{{port}}/health","mode":"Probe","timeoutSeconds":3}"""));

        result.Status.ShouldBe(ServiceHealth.Healthy);
        await _probe.Received(1).RunAsync(
            Arg.Is<ProbeRequest>(r =>
                r.NetworkName == "haven-p-e" &&
                r.Entrypoint == "curl" &&
                r.Args.Contains("http://haven-p-e-api:8080/health") &&
                r.Timeout == TimeSpan.FromSeconds(3)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Http_Probe_NoContainer_IsUnknownContainerNotFound()
    {
        _resolver.ResolveAsync(ServiceId, Arg.Any<CancellationToken>()).Returns(Result<HealthCheckTarget>.Failure(Error.Docker.ContainerNotFound));

        var result = await _http.RunHealthCheckAsync(Http("""{"url":"http://{{container}}/","mode":"Probe"}"""));

        result.Status.ShouldBe(ServiceHealth.Unknown);
        result.Reason.ShouldBe(HealthCheckFailureReason.ContainerNotFound);
        await _probe.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
    }

    [Test]
    public async Task Http_Probe_StoppedContainer_IsUnhealthyWithoutProbing()
    {
        Target(Running() with { IsRunning = false });

        var result = await _http.RunHealthCheckAsync(Http("""{"url":"http://{{container}}/","mode":"Probe"}"""));

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.ContainerNotRunning);
        await _probe.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
    }

    [Test]
    public async Task Http_Probe_ProbeCannotRun_IsUnknownNotUnhealthy()
    {
        Target(Running());
        _probe.RunAsync(Arg.Any<ProbeRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProbeOutput>.Failure(Error.Docker.OperationFailed("cannot pull image")));

        var result = await _http.RunHealthCheckAsync(Http("""{"url":"http://{{container}}/","mode":"Probe"}"""));

        result.Status.ShouldBe(ServiceHealth.Unknown);
        result.Reason.ShouldBe(HealthCheckFailureReason.ProbeUnavailable);
        result.Message.ShouldContain("cannot pull image");
    }

    [Test]
    public async Task Http_Probe_UnknownPlaceholder_IsInvalidConfig()
    {
        Target(Running());

        var result = await _http.RunHealthCheckAsync(Http("""{"url":"http://{{host}}/","mode":"Probe"}"""));

        result.Status.ShouldBe(ServiceHealth.Unknown);
        result.Reason.ShouldBe(HealthCheckFailureReason.InvalidConfig);
    }

    [Test]
    public async Task Http_Probe_NoNetwork_IsUnknownProbeUnavailable()
    {
        Target(Running(network: null));

        var result = await _http.RunHealthCheckAsync(Http("""{"url":"http://{{container}}/","mode":"Probe"}"""));

        result.Status.ShouldBe(ServiceHealth.Unknown);
        result.Reason.ShouldBe(HealthCheckFailureReason.ProbeUnavailable);
    }

    // ---- TCP ----------------------------------------------------------------------------------------------------

    [Test]
    public async Task Tcp_InvalidPort_IsUnknownInvalidConfig()
    {
        var result = await _tcp.RunHealthCheckAsync(Tcp("""{"host":"{{container}}","port":0}"""));

        result.Status.ShouldBe(ServiceHealth.Unknown);
        result.Reason.ShouldBe(HealthCheckFailureReason.InvalidConfig);
    }

    [Test]
    public async Task Tcp_Open_IsHealthyAndUsesNcOnTargetNetwork()
    {
        Target(Running());
        ProbeReturns(new ProbeOutput(0, "", "", 20));

        var result = await _tcp.RunHealthCheckAsync(Tcp("""{"host":"{{container}}","port":5432,"timeoutSeconds":4}"""));

        result.Status.ShouldBe(ServiceHealth.Healthy);
        await _probe.Received(1).RunAsync(
            Arg.Is<ProbeRequest>(r => r.Entrypoint == "nc" && r.NetworkName == "haven-p-e" && r.Args.SequenceEqual(new[] { "-z", "-w", "4", "haven-p-e-api", "5432" })),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Tcp_Closed_IsUnhealthyConnectionRefused()
    {
        Target(Running());
        ProbeReturns(new ProbeOutput(1, "", "", 25));

        var result = await _tcp.RunHealthCheckAsync(Tcp("""{"host":"{{container}}","port":5432}"""));

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.ConnectionRefused);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private Func<HttpResponseMessage> _next = () => new HttpResponseMessage(HttpStatusCode.OK);

        public void Respond(HttpStatusCode status) => _next = () => new HttpResponseMessage(status);
        public void Throw(Exception ex) => _next = () => throw ex;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_next());
    }
}
