using Haven.Presentation.Api.Bootstrapping;
using Haven.Presentation.Api.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Haven.Integration.Tests.Api;

[TestFixture]
public class WebhookPortGateMiddlewareTests
{
    private const int MainPort = 8080;
    private const int WebhookPort = 8888;

    private static async Task<(int Status, bool NextCalled)> Send(int localPort, string path)
    {
        var nextCalled = false;
        var middleware = new WebhookPortGateMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            new WebhookPortOptions(MainPort, WebhookPort),
            NullLogger<WebhookPortGateMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Connection.LocalPort = localPort;
        context.Request.Path = path;

        await middleware.InvokeAsync(context);
        return (context.Response.StatusCode, nextCalled);
    }

    [TestCase("/webhooks/deploy/abc")]
    [TestCase("/webhooks/action/abc")]
    public async Task WebhookPort_WebhookPath_PassesThrough(string path)
    {
        var (_, nextCalled) = await Send(WebhookPort, path);
        nextCalled.ShouldBeTrue();
    }

    [TestCase("/api/projects")]
    [TestCase("/scalar/v1")]
    [TestCase("/webhooksevil")]
    [TestCase("/webhook/deploy/abc")]
    [TestCase("/")]
    public async Task WebhookPort_NonWebhookPath_Returns404(string path)
    {
        var (status, nextCalled) = await Send(WebhookPort, path);
        status.ShouldBe(StatusCodes.Status404NotFound);
        nextCalled.ShouldBeFalse();
    }

    [TestCase("/api/projects")]
    [TestCase("/webhooks/deploy/abc")]
    public async Task MainPort_AnyPath_PassesThrough(string path)
    {
        var (_, nextCalled) = await Send(MainPort, path);
        nextCalled.ShouldBeTrue();
    }

    private static IConfiguration Config(params (string, string)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Item1, v.Item2)))
            .Build();

    [Test]
    public void Options_Defaults()
    {
        var options = WebhookPortOptions.From(Config());
        options.MainPort.ShouldBe(8080);
        options.WebhookPort.ShouldBe(8888);
    }

    [Test]
    public void Options_InvalidPort_Throws() =>
        Should.Throw<InvalidOperationException>(() => WebhookPortOptions.From(Config(("WEBHOOK_PORT", "abc"))));

    [Test]
    public void Options_SamePort_Throws() =>
        Should.Throw<InvalidOperationException>(() =>
            WebhookPortOptions.From(Config(("PORT", "9000"), ("WEBHOOK_PORT", "9000"))));
}
