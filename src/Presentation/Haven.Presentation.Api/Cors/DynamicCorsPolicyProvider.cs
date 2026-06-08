using Haven.Application.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace Haven.Presentation.Api.Cors;

public sealed class DynamicCorsPolicyProvider(IOptionsMonitor<NetworkOptions> networkOptions) : ICorsPolicyProvider
{
    public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var options = networkOptions.CurrentValue;
        var builder = new CorsPolicyBuilder();

        if (options.Domains.Count == 0)
        {
            builder.SetIsOriginAllowed(_ => true);
        }
        else
        {
            var origins = options.Domains.Select(d => BuildOrigin(d, options)).ToArray();
            builder.WithOrigins(origins);
        }

        builder.AllowAnyMethod().AllowAnyHeader().AllowCredentials();

        return Task.FromResult<CorsPolicy?>(builder.Build());
    }

    private static string BuildOrigin(string domain, NetworkOptions options)
    {
        var scheme = options.EnableTls ? "https" : "http";
        var defaultPort = options.EnableTls ? 443 : 80;
        return options.Port != defaultPort
            ? $"{scheme}://{domain}:{options.Port}"
            : $"{scheme}://{domain}";
    }
}
