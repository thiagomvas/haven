using System.Formats.Tar;
using System.Text.RegularExpressions;

using Docker.DotNet.Models;

using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

namespace Haven.Infrastructure.Utils;

public static class DockerUtils
{
    private const int MaxLength = 63;
    private const string Prefix = "haven-";
    private const int GuidLength = 12;

    public static KeyValuePair<string, string> HavenManagedLabel
        => new KeyValuePair<string, string>("haven.managed", "true");

    /// <summary>
    /// Normalizes a string to be Docker-name safe.
    /// </summary>
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "unknown";

        input = input.ToLowerInvariant();

        // Replace invalid characters with '-'
        input = Regex.Replace(input, @"[^a-z0-9_.-]", "-");

        // Collapse multiple '-'
        input = Regex.Replace(input, @"-+", "-");

        return input.Trim('-');
    }

    /// <summary>
    /// Builds a Docker-safe container name: haven-{projectAlias}-{envAlias}-{serviceAlias}
    /// Falls back to the legacy name format when aliases are not available.
    /// </summary>
    public static string BuildContainerName(string? projectAlias, string? envAlias, string? serviceAlias, string serviceName, Guid serviceId)
    {
        if (!string.IsNullOrEmpty(projectAlias) && !string.IsNullOrEmpty(envAlias) && !string.IsNullOrEmpty(serviceAlias))
            return $"{Prefix}{projectAlias}-{envAlias}-{serviceAlias}";

        // Legacy fallback: haven-{name}-{shortId}
        var name = Normalize(serviceName);
        var rawId = serviceId.ToString("N").ToLowerInvariant();
        var shortId = rawId[..Math.Min(GuidLength, rawId.Length)];

        int reserved = Prefix.Length + 1 + shortId.Length;
        int maxNameLength = MaxLength - reserved;

        if (maxNameLength <= 0)
            throw new InvalidOperationException("Container naming constraints exceeded.");

        if (name.Length > maxNameLength)
            name = name[..maxNameLength].Trim('-');

        return $"{Prefix}{name}-{shortId}";
    }

    public static Dictionary<string, string> BuildContainerLabels(Service service)
    {
        var idLabel = BuildIdLabel(service.Id);
        var managedLabel = HavenManagedLabel;
        var dict = new Dictionary<string, string>
        {
            { HavenManagedLabel.Key, HavenManagedLabel.Value },
            { "haven.service.name", service.Name },
            { idLabel.Key, idLabel.Value }
        };

        if (service.Environment is not null && !string.IsNullOrWhiteSpace(service.Environment.Name))
            dict.Add("haven.environment.name", service.Environment.Name);

        if (service.Environment?.Project is not null && !string.IsNullOrWhiteSpace(service.Environment.Project.Name))
            dict.Add("haven.project.name", service.Environment.Project.Name);

        return dict;
    }

    public static KeyValuePair<string, string> BuildIdLabel(Guid id)
    {
        return new KeyValuePair<string, string>("haven.service.id", id.ToString());
    }


    public static string BuildNetworkName(string? projectAlias, string? envAlias, string projectName, string environmentName)
    {
        if (!string.IsNullOrEmpty(projectAlias) && !string.IsNullOrEmpty(envAlias))
            return $"{Prefix}{projectAlias}-{envAlias}";

        // Legacy fallback
        var sanitized = $"haven-{SanitizeForDocker(projectName)}-{SanitizeForDocker(environmentName)}";
        return sanitized.Length > 64 ? sanitized[..64] : sanitized;
    }

    public static string GenerateSubnetForEnvironment(Guid projectId, Guid environmentId)
    {
        // Use the first 2 bytes of the IDs to create a unique subnet
        var projectBytes = projectId.ToByteArray();
        var envBytes = environmentId.ToByteArray();

        // Combine bytes to generate a number between 0-65535
        var subnetSecond = BitConverter.ToUInt16(projectBytes, 0) % 4096; // 0-4095 (fits in /12 range)
        var subnetThird = BitConverter.ToUInt16(envBytes, 0) % 256; // 0-255

        // Subnet in 172.16.0.0/12 range: 172.16-31.x.0/24
        var baseSecond = 16 + (subnetSecond / 256);
        var baseThird = subnetSecond % 256;

        return $"172.{baseSecond}.{baseThird}.0/24";
    }

    /// <summary>
    /// Derives the first usable host address (the conventional gateway) for a /24 CIDR block,
    /// e.g. "172.16.5.0/24" -> "172.16.5.1".
    /// </summary>
    public static string DeriveGatewayFromSubnet(string cidr)
    {
        var networkAddress = cidr.Split('/')[0];
        var octets = networkAddress.Split('.');
        octets[3] = "1";
        return string.Join('.', octets);
    }

    public static string SanitizeForDocker(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            input.ToLowerInvariant(),
            "[^a-z0-9._-]",
            "-");
    }

    public static bool IsValidNetworkName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 64)
            return false;

        return System.Text.RegularExpressions.Regex.IsMatch(
            name,
            "^[a-z0-9]([a-z0-9-]{0,62}[a-z0-9])?$");
    }

    public static string BuildImageTag(string? projectAlias, string? envAlias, string? serviceAlias, Guid serviceId)
    {
        if (!string.IsNullOrEmpty(projectAlias) && !string.IsNullOrEmpty(envAlias) && !string.IsNullOrEmpty(serviceAlias))
            return $"{Prefix}{projectAlias}-{envAlias}-{serviceAlias}";

        return $"haven-service-{serviceId:N}";
    }

    /// <summary>
    /// Builds the Docker <see cref="Mount"/> list for a service's volumes. Managed volumes are
    /// bind-mounted; their backing directory is created (if missing) at
    /// <paramref name="volumesRootLocal"/> — the path as seen by Haven's own process — while the
    /// <see cref="Mount.Source"/> given to the Docker daemon uses <paramref name="volumesRootHost"/>,
    /// which may differ when Haven runs Docker-outside-of-Docker (see <see cref="Haven.Application.Common.Interfaces.Deployment.IHostPathResolver"/>).
    /// </summary>
    public static List<Mount> BuildMounts(Service service, string volumesRootLocal, string volumesRootHost)
    {
        var mounts = new List<Mount>();

        foreach (var volume in service.Volumes)
        {
            var mount = new Mount { Target = volume.Target, ReadOnly = volume.ReadOnly };

            switch (volume.Type)
            {
                case VolumeType.Named:
                    mount.Type = "volume";
                    mount.Source = volume.Source;
                    break;

                case VolumeType.HostPath:
                    mount.Type = "bind";
                    mount.Source = volume.Source;
                    break;

                case VolumeType.Managed:
                    mount.Type = "bind";
                    var localPath = ManagedVolumeHostPath(volumesRootLocal, service.Id, volume.Id);
                    Directory.CreateDirectory(localPath);
                    mount.Source = ManagedVolumeHostPath(volumesRootHost, service.Id, volume.Id);
                    break;
            }

            mounts.Add(mount);
        }

        return mounts;
    }

    /// <summary>
    /// Resolves the absolute host directory that backs a managed volume:
    /// <c>{volumesRoot}/{serviceId}/{volumeId}</c>. The path is made absolute so the Docker
    /// daemon can bind-mount it.
    /// </summary>
    public static string ManagedVolumeHostPath(string volumesRoot, Guid serviceId, Guid volumeId) =>
        Path.GetFullPath(Path.Combine(volumesRoot, serviceId.ToString(), volumeId.ToString()));

    public static List<PortMapping> ExtractPortMappings(this ContainerInspectResponse inspect)
    {
        var result = new List<PortMapping>();
        if (inspect.NetworkSettings.Ports is null) return result;

        foreach (var (containerPortProto, bindings) in inspect.NetworkSettings.Ports)
        {
            if (!int.TryParse(containerPortProto.Split('/')[0], out var containerPort)) continue;

            if (bindings is null or { Count: 0 })
            {
                result.Add(new PortMapping(null, containerPort));
                continue;
            }

            foreach (var binding in bindings)
            {
                var hostPort = int.TryParse(binding.HostPort, out var p) ? p : (int?)null;
                var hostIp = string.IsNullOrEmpty(binding.HostIP) ? null : binding.HostIP;
                result.Add(new PortMapping(hostPort, containerPort, hostIp));
            }
        }

        return result;
    }

    public static async Task<Stream> CreateTarArchiveFromDirectoryAsync(string directory, CancellationToken cancellationToken)
    {
        var directoryInfo = new DirectoryInfo(directory);
        if (!directoryInfo.Exists)
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

        var files = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).ToList();
        if (files.Count == 0)
            throw new InvalidOperationException($"No files found in directory: {directory}");

        var memoryStream = new MemoryStream();
        await using (var tarWriter = new TarWriter(memoryStream, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(directory, file.FullName).Replace("\\", "/");
                if (relativePath.StartsWith('/'))
                    relativePath = relativePath[1..];

                using var fileStream = file.OpenRead();
                var fileBytes = new byte[fileStream.Length];
                _ = await fileStream.ReadAsync(fileBytes, cancellationToken);

                var entry = new PaxTarEntry(TarEntryType.RegularFile, relativePath)
                {
                    DataStream = new MemoryStream(fileBytes)
                };
                await tarWriter.WriteEntryAsync(entry, cancellationToken);
            }
        } // TarWriter disposed here — end-of-archive written at end of data

        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }

    public static async Task<Stream> CreateTarArchiveFromContentAsync(string dockerfileContent, CancellationToken cancellationToken)
    {
        var memoryStream = new MemoryStream();
        await using (var tarWriter = new TarWriter(memoryStream, leaveOpen: true))
        {
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(dockerfileContent);
            var entry = new PaxTarEntry(TarEntryType.RegularFile, "Dockerfile")
            {
                DataStream = new MemoryStream(contentBytes)
            };
            await tarWriter.WriteEntryAsync(entry, cancellationToken);
        } // TarWriter disposed here — end-of-archive written at end of data

        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }

    /// <summary>Projects environment variables into "KEY=VALUE" strings for a container's Env list.</summary>
    public static List<string> BuildEnvironmentVariableStrings(IEnumerable<EnvironmentVariables>? envs) =>
        (envs ?? []).Select(e => $"{e.Key}={e.Value}").ToList();

    /// <summary>Resolves the LISTEN_ADDRESS a container should bind to for a given exposure mode, or null when the mode needs none.</summary>
    public static string? TryBuildListenAddress(ExposureMode exposureMode) =>
        exposureMode switch
        {
            ExposureMode.Internal => "127.0.0.1",
            ExposureMode.External or ExposureMode.Custom => "0.0.0.0",
            _ => null
        };

    /// <summary>
    /// Parses "hostPort:containerPort" (or "hostIp:hostPort:containerPort" in <see cref="ExposureMode.Custom"/>)
    /// mappings into Docker exposed-ports/port-bindings dictionaries. Malformed entries are skipped and
    /// reported via <see cref="PortBindingResult.Warnings"/> instead of being logged, keeping this a pure function.
    /// </summary>
    public static PortBindingResult BuildPortBindings(IEnumerable<string> portMappings, ExposureMode exposureMode, string listenAddress)
    {
        var exposedPorts = new Dictionary<string, EmptyStruct>();
        var portBindings = new Dictionary<string, IList<PortBinding>>();
        var warnings = new List<string>();

        foreach (var portMapping in portMappings)
        {
            var parts = portMapping.Split(':');
            if (parts.Length < 2)
            {
                warnings.Add($"Invalid port mapping format: {portMapping}. Expected 'hostPort:containerPort' or 'hostIp:hostPort:containerPort'");
                continue;
            }

            string hostIp;
            string hostPort;
            string containerPort;
            if (parts.Length >= 3 && exposureMode == ExposureMode.Custom)
            {
                hostIp = parts[0];
                hostPort = parts[1];
                containerPort = parts[2];
            }
            else
            {
                hostIp = listenAddress;
                hostPort = parts[0];
                containerPort = parts[1];
            }

            var portKey = containerPort.Contains('/') ? containerPort : $"{containerPort}/tcp";
            exposedPorts[portKey] = default;
            portBindings[portKey] = new List<PortBinding>
            {
                new PortBinding { HostIP = hostIp, HostPort = hostPort }
            };
        }

        return new PortBindingResult(exposedPorts, portBindings, warnings);
    }
}

/// <summary>Result of parsing port mappings via <see cref="DockerUtils.BuildPortBindings"/>.</summary>
public sealed record PortBindingResult(
    Dictionary<string, EmptyStruct> ExposedPorts,
    Dictionary<string, IList<PortBinding>> PortBindings,
    IReadOnlyList<string> Warnings);