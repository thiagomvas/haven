using System.Net;

using Haven.Application.Common;
using Haven.Application.Features.CustomActions.Services;
using Haven.Domain.ValueObjects;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Haven.Application.Tests.Features.CustomActions;

[Category("Unit")]
public sealed class HttpActionStrategyTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return await respond(request, ct);
        }
    }

    private static StubHandler Responding(HttpStatusCode code) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(code)));

    private static HttpActionStrategy CreateSut(StubHandler handler) =>
        new(new HttpClient(handler), NullLogger<HttpActionStrategy>.Instance);

    [Test]
    public void CanHandle_HttpConfig_ReturnsTrue()
    {
        var sut = CreateSut(Responding(HttpStatusCode.OK));
        sut.CanHandle(CustomActionTestData.Create(CustomActionTestData.Http())).ShouldBeTrue();
    }

    [Test]
    public void CanHandle_ExecConfig_ReturnsFalse()
    {
        var sut = CreateSut(Responding(HttpStatusCode.OK));
        var action = CustomActionTestData.Create(new ExecActionConfig(["ls"], null, null, null));
        sut.CanHandle(action).ShouldBeFalse();
    }

    [Test]
    public async Task ExecuteAsync_NonHttpConfig_ReturnsNotSupported()
    {
        var sut = CreateSut(Responding(HttpStatusCode.OK));
        var action = CustomActionTestData.Create(new ExecActionConfig(["ls"], null, null, null));

        var result = await sut.ExecuteAsync(action, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NotSupported);
    }

    [TestCase("not a url")]
    [TestCase("/relative/path")]
    [TestCase("ftp://example.com/file")]
    public async Task ExecuteAsync_InvalidUrl_ReturnsValidationError(string url)
    {
        var handler = Responding(HttpStatusCode.OK);
        var action = CustomActionTestData.Create(CustomActionTestData.Http(url: url));

        var result = await CreateSut(handler).ExecuteAsync(action, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("VALIDATION");
        handler.Request.ShouldBeNull();
    }

    [Test]
    public async Task ExecuteAsync_2xxResponse_ReturnsSuccessWithStatusCode()
    {
        var action = CustomActionTestData.Create(CustomActionTestData.Http());

        var result = await CreateSut(Responding(HttpStatusCode.Accepted)).ExecuteAsync(action, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.StatusCode.ShouldBe(202);
    }

    [Test]
    public async Task ExecuteAsync_SendsConfiguredMethodUrlHeadersAndBody()
    {
        var handler = Responding(HttpStatusCode.OK);
        var config = CustomActionTestData.Http(HttpMethod.Put, "https://example.com/a?b=1",
            new Dictionary<string, string> { ["X-Token"] = "abc" }, "{\"x\":1}");

        await CreateSut(handler).ExecuteAsync(CustomActionTestData.Create(config), CancellationToken.None);

        handler.Request!.Method.ShouldBe(HttpMethod.Put);
        handler.Request.RequestUri!.ToString().ShouldBe("https://example.com/a?b=1");
        handler.Request.Headers.GetValues("X-Token").ShouldBe(["abc"]);
        handler.RequestBody.ShouldBe("{\"x\":1}");
        handler.Request.Content!.Headers.ContentType!.MediaType.ShouldBe("application/json");
    }

    [Test]
    public async Task ExecuteAsync_ContentTypeHeader_OverridesDefaultContentType()
    {
        var handler = Responding(HttpStatusCode.OK);
        var config = CustomActionTestData.Http(headers: new Dictionary<string, string> { ["Content-Type"] = "text/plain" },
            body: "hello");

        await CreateSut(handler).ExecuteAsync(CustomActionTestData.Create(config), CancellationToken.None);

        handler.Request!.Content!.Headers.ContentType!.MediaType.ShouldBe("text/plain");
    }

    [Test]
    public async Task ExecuteAsync_NoBody_SendsNoContent()
    {
        var handler = Responding(HttpStatusCode.OK);

        await CreateSut(handler).ExecuteAsync(
            CustomActionTestData.Create(CustomActionTestData.Http(HttpMethod.Get)), CancellationToken.None);

        handler.Request!.Content.ShouldBeNull();
    }

    [Test]
    public async Task ExecuteAsync_Non2xxResponse_ReturnsFailure()
    {
        var action = CustomActionTestData.Create(CustomActionTestData.Http());

        var result = await CreateSut(Responding(HttpStatusCode.InternalServerError))
            .ExecuteAsync(action, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(Error.Failed.Code);
        result.Error.Message.ShouldContain("500");
    }

    [Test]
    public async Task ExecuteAsync_CustomSuccessCodes_AcceptsListedNon2xxCode()
    {
        var action = CustomActionTestData.Create(CustomActionTestData.Http(successCodes: [404]));

        var result = await CreateSut(Responding(HttpStatusCode.NotFound)).ExecuteAsync(action, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task ExecuteAsync_CustomSuccessCodes_RejectsUnlisted2xxCode()
    {
        var action = CustomActionTestData.Create(CustomActionTestData.Http(successCodes: [201]));

        var result = await CreateSut(Responding(HttpStatusCode.OK)).ExecuteAsync(action, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task ExecuteAsync_EmptySuccessCodes_FallsBackTo2xx()
    {
        var action = CustomActionTestData.Create(CustomActionTestData.Http(successCodes: []));

        var result = await CreateSut(Responding(HttpStatusCode.OK)).ExecuteAsync(action, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task ExecuteAsync_HttpRequestException_ReturnsFailure()
    {
        var handler = new StubHandler((_, _) => throw new HttpRequestException("boom"));
        var action = CustomActionTestData.Create(CustomActionTestData.Http());

        var result = await CreateSut(handler).ExecuteAsync(action, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("boom");
    }

    [Test]
    public async Task ExecuteAsync_ExceedsTimeout_ReturnsTimeoutFailure()
    {
        var handler = new StubHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var action = CustomActionTestData.Create(CustomActionTestData.Http(), TimeSpan.FromMilliseconds(50));

        var result = await CreateSut(handler).ExecuteAsync(action, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(Error.Failed.Code);
        result.Error.Message.ShouldContain("timed out");
    }

    [Test]
    public async Task ExecuteAsync_CallerCancels_ReturnsCancelledOperation()
    {
        using var cts = new CancellationTokenSource();
        var handler = new StubHandler(async (_, ct) =>
        {
            await cts.CancelAsync();
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var action = CustomActionTestData.Create(CustomActionTestData.Http());

        var result = await CreateSut(handler).ExecuteAsync(action, cts.Token);

        result.Error.ShouldBe(Error.CancelledOperation);
    }
}
