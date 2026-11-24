using Haven.Application.Configuration;
using Haven.Presentation.Api.Cors;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Integration.Tests.Api;

[TestFixture]
public class DynamicCorsPolicyProviderTests
{
    private static async Task<Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicy> GetPolicy(
        NetworkOptions options, string environmentName)
    {
        var monitor = Substitute.For<IOptionsMonitor<NetworkOptions>>();
        monitor.CurrentValue.Returns(options);
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(environmentName);

        var policy = await new DynamicCorsPolicyProvider(monitor, env).GetPolicyAsync(new DefaultHttpContext(), null);
        return policy!;
    }

    [Test]
    public async Task NoDomains_Production_AllowsNoOrigin()
    {
        var policy = await GetPolicy(new NetworkOptions(), Environments.Production);

        policy.AllowAnyOrigin.ShouldBeFalse();
        policy.Origins.ShouldBeEmpty();
        policy.SupportsCredentials.ShouldBeFalse();
        policy.IsOriginAllowed("https://evil.example").ShouldBeFalse();
    }

    [Test]
    public async Task NoDomains_Development_AllowsOnlyViteDevServers()
    {
        var policy = await GetPolicy(new NetworkOptions(), Environments.Development);

        policy.IsOriginAllowed("http://localhost:5173").ShouldBeTrue();
        policy.IsOriginAllowed("http://localhost:5174").ShouldBeTrue();
        policy.IsOriginAllowed("https://evil.example").ShouldBeFalse();
    }

    [Test]
    public async Task ConfiguredDomains_AllowOnlyThoseOrigins_WithCredentials()
    {
        var options = new NetworkOptions { Domains = ["haven.example.com"], EnableTls = true, Port = 443 };

        var policy = await GetPolicy(options, Environments.Production);

        policy.IsOriginAllowed("https://haven.example.com").ShouldBeTrue();
        policy.IsOriginAllowed("https://evil.example").ShouldBeFalse();
        policy.IsOriginAllowed("http://localhost:5173").ShouldBeFalse();
        policy.SupportsCredentials.ShouldBeTrue();
    }

    [Test]
    public async Task ConfiguredDomainWithScheme_IsNotDoublePrefixed()
    {
        var options = new NetworkOptions { Domains = ["https://haven.example.com/"] };

        var policy = await GetPolicy(options, Environments.Production);

        policy.IsOriginAllowed("https://haven.example.com").ShouldBeTrue();
    }

    [Test]
    public async Task ConfiguredDomains_NonDefaultPort_IncludedInOrigin()
    {
        var options = new NetworkOptions { Domains = ["haven.local"], Port = 8080 };

        var policy = await GetPolicy(options, Environments.Production);

        policy.IsOriginAllowed("http://haven.local:8080").ShouldBeTrue();
        policy.IsOriginAllowed("http://haven.local").ShouldBeFalse();
    }
}