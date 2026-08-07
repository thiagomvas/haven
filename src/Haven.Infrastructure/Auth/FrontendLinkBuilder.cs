using Haven.Application.Common.Interfaces;
using Haven.Application.Configuration;

using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.Auth;

public class FrontendLinkBuilder(IOptionsMonitor<NetworkOptions> networkOptions) : IFrontendLinkBuilder
{
    private const string DevFallbackBaseUrl = "http://localhost:5173";

    public string BuildAcceptInviteUrl(string rawToken)
    {
        var baseUrl = networkOptions.CurrentValue.BuildHost() ?? DevFallbackBaseUrl;
        return $"{baseUrl}/accept-invite?token={Uri.EscapeDataString(rawToken)}";
    }
}
