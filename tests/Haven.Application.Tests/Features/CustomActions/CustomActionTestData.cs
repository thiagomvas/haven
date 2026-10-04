using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Tests.Features.CustomActions;

internal static class CustomActionTestData
{
    public static CustomAction Create(ActionConfig config, TimeSpan? timeout = null, Guid? serviceId = null) =>
        CustomAction.Create(serviceId ?? Guid.NewGuid(), "Restart cache", "restart-cache", "Restarts the cache",
            "refresh", config, [], ActionRisk.Safe, timeout ?? TimeSpan.FromSeconds(5));

    public static HttpActionConfig Http(HttpMethod? method = null, string url = "https://example.com/hook",
        Dictionary<string, string>? headers = null, string? body = null, int[]? successCodes = null) =>
        new(method ?? HttpMethod.Post, url, headers ?? new Dictionary<string, string>(), body, successCodes);
}
