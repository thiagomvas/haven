using System.Text.RegularExpressions;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Utils;

public static class DockerUtils
{
    private const int MaxLength = 63;
    private const string Prefix = "haven-";
    private const int GuidLength = 12; // adjust (8–12 recommended)

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
        var dict = new Dictionary<string, string>
        {
            { "haven.service.id", service.Id.ToString() },
            { "haven.service.name", service.Name }
        };

        if (service.Environment is not null && !string.IsNullOrWhiteSpace(service.Environment.Name))
            dict.Add("haven.environment.name", service.Environment.Name);
        
        if (service.Environment?.Project is not null && !string.IsNullOrWhiteSpace(service.Environment.Project.Name))
            dict.Add("haven.project.name", service.Environment.Project.Name);

        return dict;
    }
}