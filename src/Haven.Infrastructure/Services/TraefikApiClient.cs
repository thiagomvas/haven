using System.Net.Http.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment.Docker;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Services;

public sealed class TraefikApiClient(
    IHttpClientFactory httpClientFactory,
    ISidecarRepository sidecarRepository,
    IDockerContainerRuntime containerRuntime,
    ILogger<TraefikApiClient> logger) : ITraefikApiClient
{
    public async Task<Result<bool>> IsReachableAsync(CancellationToken ct = default)
    {
        var baseUrlResult = await ResolveBaseUrlAsync(ct);
        if (baseUrlResult.IsFailure)
            return baseUrlResult.Error;

        try
        {
            var client = httpClientFactory.CreateClient(nameof(TraefikApiClient));
            using var response = await client.GetAsync($"{baseUrlResult.Value}/api/overview", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return false;
        }
    }

    public async Task<Result<TraefikRouterInfo>> GetRouterInfoAsync(string routerName, CancellationToken ct = default)
    {
        var baseUrlResult = await ResolveBaseUrlAsync(ct);
        if (baseUrlResult.IsFailure)
            return baseUrlResult.Error;

        try
        {
            var client = httpClientFactory.CreateClient(nameof(TraefikApiClient));
            var routerId = $"{routerName}@docker";
            using var response = await client.GetAsync($"{baseUrlResult.Value}/api/http/routers/{routerId}", ct);

            if (!response.IsSuccessStatusCode)
                return Error.NotFoundFor("Traefik router", Guid.Empty);

            var payload = await response.Content.ReadFromJsonAsync<TraefikRouterApiResponse>(ct);
            if (payload is null)
                return Error.Failed;

            return new TraefikRouterInfo
            {
                Name = payload.Name ?? routerName,
                Status = payload.Status ?? "unknown",
                HasTls = payload.Tls is not null,
                Errors = payload.Error ?? []
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            logger.LogDebug(ex, "Failed to reach Traefik's API to fetch router '{RouterName}'", routerName);
            return Error.Failed;
        }
    }

    private async Task<Result<string>> ResolveBaseUrlAsync(CancellationToken ct)
    {
        var sidecars = await sidecarRepository.GetAllAsync(ct);
        var traefik = sidecars.FirstOrDefault(s => s.Kind == SidecarKind.Traefik);
        if (traefik is not { Enabled: true })
            return Error.NotFoundFor("Traefik sidecar", Guid.Empty);

        var inspectResult = await containerRuntime.InspectByServiceIdAsync(traefik.Id, ct);
        if (inspectResult.IsFailure)
            return inspectResult.Error;

        var containerName = inspectResult.Value.Name.TrimStart('/');
        return $"http://{containerName}:{DockerUtils.TraefikHavenApiPort}";
    }

    private sealed class TraefikRouterApiResponse
    {
        public string? Name { get; set; }
        public string? Status { get; set; }
        public object? Tls { get; set; }
        public List<string>? Error { get; set; }
    }
}
