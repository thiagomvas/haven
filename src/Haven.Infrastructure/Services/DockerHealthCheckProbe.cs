using System.Diagnostics;

using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.Services;

/// <summary>
/// Runs curl/nc in a short-lived container attached to the target's network. Haven itself never joins that network.
/// </summary>
public sealed class DockerHealthCheckProbe(
    IDockerClient dockerClient,
    IOptionsMonitor<HealthCheckOptions> options,
    ILogger<DockerHealthCheckProbe> logger) : IHealthCheckProbe
{
    public const string ProbeLabel = "haven.healthcheck.probe";
    private const long ProbeMemoryBytes = 64 * 1024 * 1024;

    public async Task<Result<ProbeOutput>> RunAsync(ProbeRequest request, CancellationToken cancellationToken = default)
    {
        var settings = options.CurrentValue;
        string? containerId = null;

        try
        {
            var image = await EnsureImageAsync(settings.ProbeImage, cancellationToken);
            if (image.IsFailure)
                return image.Error;

            var created = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = settings.ProbeImage,
                Entrypoint = [request.Entrypoint],
                Cmd = request.Args.ToList(),
                Labels = new Dictionary<string, string> { [ProbeLabel] = "true" },
                HostConfig = new HostConfig
                {
                    NetworkMode = request.NetworkName,
                    Memory = ProbeMemoryBytes,
                    RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.No }
                },
                NetworkingConfig = new NetworkingConfig
                {
                    EndpointsConfig = new Dictionary<string, EndpointSettings>
                    {
                        [request.NetworkName] = new EndpointSettings()
                    }
                }
            }, cancellationToken);
            containerId = created.ID;

            var stopwatch = Stopwatch.StartNew();
            await dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), cancellationToken);

            using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            waitCts.CancelAfter(request.Timeout + TimeSpan.FromSeconds(Math.Max(1, settings.ProbeStartupGraceSeconds)));

            ContainerWaitResponse wait;
            try
            {
                wait = await dockerClient.Containers.WaitContainerAsync(containerId, waitCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Error.Docker.OperationFailed("The health check probe container did not finish in time.");
            }

            stopwatch.Stop();

            using var logs = await dockerClient.Containers.GetContainerLogsAsync(
                containerId,
                false,
                new ContainerLogsParameters { ShowStdout = true, ShowStderr = true },
                cancellationToken);
            var (stdout, stderr) = await logs.ReadOutputToEndAsync(cancellationToken);

            return new ProbeOutput(wait.StatusCode, stdout, stderr, stopwatch.ElapsedMilliseconds);
        }
        catch (DockerApiException ex)
        {
            logger.LogWarning(ex, "Health check probe failed to run on network '{Network}'", request.NetworkName);
            return Error.Docker.OperationFailed($"Could not run the health check probe container: {ex.Message}");
        }
        finally
        {
            if (containerId is not null)
                await RemoveQuietlyAsync(containerId);
        }
    }

    private async Task<Result> EnsureImageAsync(string image, CancellationToken cancellationToken)
    {
        try
        {
            await dockerClient.Images.InspectImageAsync(image, cancellationToken);
            return Result.Success();
        }
        catch (DockerImageNotFoundException)
        {
            // Not present locally; pull below.
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Not present locally; pull below.
        }

        try
        {
            logger.LogInformation("Pulling health check probe image '{Image}'", image);
            await dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = image },
                null,
                new Progress<JSONMessage>(),
                cancellationToken);
            return Result.Success();
        }
        catch (DockerApiException ex)
        {
            logger.LogWarning(ex, "Failed to pull health check probe image '{Image}'", image);
            return Error.Docker.OperationFailed($"Could not pull the health check probe image '{image}': {ex.Message}");
        }
    }

    private async Task RemoveQuietlyAsync(string containerId)
    {
        try
        {
            await dockerClient.Containers.RemoveContainerAsync(
                containerId,
                new ContainerRemoveParameters { Force = true },
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to remove health check probe container '{ContainerId}'", containerId);
        }
    }
}
