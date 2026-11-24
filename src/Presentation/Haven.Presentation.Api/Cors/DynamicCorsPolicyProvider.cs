using Haven.Application.Configuration;

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace Haven.Presentation.Api.Cors;

/// <summary>
/// Builds the CORS policy from the configured domains. The bundled web UI is served by the API itself
/// (same-origin), so it never needs CORS. With no domains configured only the local Vite dev servers
/// are allowed, and only in Development; otherwise the policy allows no cross-origin caller at all.
/// Credentialed requests are never allowed from an arbitrary origin.
/// </summary>
public sealed class DynamicCorsPolicyProvider(
    IOptionsMonitor<NetworkOptions> networkOptions,
    IHostEnvironment environment) : ICorsPolicyProvider
{
    // Vite dev servers (see CLAUDE.md): the UI runs on a different origin than the API in development.
    private static readonly string[] DevelopmentOrigins =
    [
        "http://localhost:5173",
        "http://localhost:5174",
        "http://127.0.0.1:5173",
        "http://127.0.0.1:5174"
    ];

    public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var options = networkOptions.CurrentValue;

        var origins = options.Domains
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(options.BuildOrigin)
            .ToList();

        if (environment.IsDevelopment())
            origins.AddRange(DevelopmentOrigins);

        var builder = new CorsPolicyBuilder();

        if (origins.Count > 0)
        {
            builder.WithOrigins(origins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }

        // No origins: an empty policy, so no Access-Control-* headers are emitted and browsers block
        // every cross-origin request. Same-origin requests are unaffected.
        return Task.FromResult<CorsPolicy?>(builder.Build());
    }
}