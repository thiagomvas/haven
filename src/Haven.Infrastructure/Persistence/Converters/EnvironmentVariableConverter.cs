using Haven.Domain;
using Haven.Domain.Entities;

using Environment = System.Environment;

namespace Haven.Infrastructure.Persistence.Converters;

public static class EnvironmentVariableConverter
{
    public static string Convert(IEnumerable<EnvironmentVariables> variables, bool includeValues = true)
    {
        var lines = variables
            .Select(ev => FormatEnvLine(ev.Key, ev.Value, includeValues))
            .Where(line => !string.IsNullOrEmpty(line));

        return string.Join(Environment.NewLine, lines);
    }

    public static List<EnvironmentVariables> Convert(string envContent, Guid parentId, EnvironmentVariableParentType parentType)
    {
        var variables = new List<EnvironmentVariables>();
        var lines = envContent.Split(new[] { Environment.NewLine, "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                continue;

            var (key, value) = ParseLine(trimmedLine);
            if (!string.IsNullOrEmpty(key))
            {
                variables.Add(new EnvironmentVariables
                {
                    Key = key,
                    Value = value,
                    ParentId = parentId,
                    ParentType = parentType
                });
            }
        }

        return variables;
    }

    private static string FormatEnvLine(string key, string? value, bool includeValues = true)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        var formattedValue = FormatValue(value);
        if (includeValues) return $"{key}={formattedValue}";
        return $"{key}=";
    }

    private static string FormatValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        if (NeedsQuoting(value))
        {
            var escaped = value.Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }

        return value;
    }

    private static bool NeedsQuoting(string value) =>
        value.Any(c => char.IsWhiteSpace(c)) ||
        value.Contains('"') ||
        value.Contains('\\') ||
        value.Contains('=') ||
        value.Contains('#');

    private static (string key, string? value) ParseLine(string line)
    {
        var equalsIndex = line.IndexOf('=');
        if (equalsIndex == -1)
            return (string.Empty, null);

        var key = line[..equalsIndex].Trim();
        var valueStr = line[(equalsIndex + 1)..];

        var value = UnquoteValue(valueStr);
        return (key, value);
    }

    private static string? UnquoteValue(string value)
    {
        var trimmedValue = value.Trim();

        if (trimmedValue == "\"\"")
            return string.Empty;

        if (trimmedValue.StartsWith('"') && trimmedValue.EndsWith('"') && trimmedValue.Length > 1)
        {
            var unquoted = trimmedValue[1..^1];
            return unquoted.Replace("\\\"", "\"");
        }

        return trimmedValue;
    }
}