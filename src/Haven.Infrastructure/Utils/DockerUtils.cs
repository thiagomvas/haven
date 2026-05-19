using System.Formats.Tar;
using System.Text.RegularExpressions;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Utils;

public static class DockerUtils
{
    private const int MaxLength = 63;
    private const string Prefix = "haven-";
    private const int GuidLength = 12; // adjust (8–12 recommended)
    
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
    /// Builds a Docker-safe container name: haven-{name}-{shortId}
    /// </summary>
    public static string BuildContainerName(string serviceName, Guid id)
    {
        var name = Normalize(serviceName);

        // Compact GUID (no dashes), then trim
        var rawId = id.ToString("N").ToLowerInvariant();
        var shortId = rawId[..Math.Min(GuidLength, rawId.Length)];

        // Calculate max allowed length for name
        int reserved = Prefix.Length + 1 + shortId.Length; // prefix + '-' + id
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


    public static string GenerateDockerNetworkName(string projectName, string environmentName)
    {
        var sanitized = $"haven-{SanitizeForDocker(projectName)}-{SanitizeForDocker(environmentName)}";

        // Docker network names must be <= 64 characters
        return sanitized.Length > 64
            ? sanitized[..64]
            : sanitized;
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

    public static string BuildImageTag(Guid serviceId)
        => $"haven-service-{serviceId:N}";

    public static async Task<Stream> CreateTarArchiveFromDirectoryAsync(string directory, CancellationToken cancellationToken)
    {
        var memoryStream = new MemoryStream();
        await using var tarStream = new TarWriter(memoryStream, leaveOpen: true);

        var directoryInfo = new DirectoryInfo(directory);
        if (!directoryInfo.Exists)
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

        // Get all files, including Dockerfile at root
        var files = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).ToList();
        if (files.Count == 0)
            throw new InvalidOperationException($"No files found in directory: {directory}");

        foreach (var file in files)
        {
            // Get path relative to directory, normalize to forward slashes
            var relativePath = Path.GetRelativePath(directory, file.FullName).Replace("\\", "/");

            // Ensure no leading slashes
            if (relativePath.StartsWith('/'))
                relativePath = relativePath[1..];

            using var fileStream = file.OpenRead();
            var fileBytes = new byte[fileStream.Length];
            _ = await fileStream.ReadAsync(fileBytes, cancellationToken);

            var entry = new PaxTarEntry(TarEntryType.RegularFile, relativePath)
            {
                DataStream = new MemoryStream(fileBytes)
            };
            await tarStream.WriteEntryAsync(entry, cancellationToken);
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }

    public static async Task<Stream> CreateTarArchiveFromContentAsync(string dockerfileContent, CancellationToken cancellationToken)
    {
        var memoryStream = new MemoryStream();
        await using var tarStream = new TarWriter(memoryStream, leaveOpen: true);

        var contentBytes = System.Text.Encoding.UTF8.GetBytes(dockerfileContent);
        var entry = new PaxTarEntry(TarEntryType.RegularFile, "Dockerfile")
        {
            DataStream = new MemoryStream(contentBytes)
        };

        await tarStream.WriteEntryAsync(entry, cancellationToken);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }
}