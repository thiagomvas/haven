namespace Haven.Domain.ValueObjects;

public sealed class Version : ValueObject, IComparable<Version>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string? PreRelease { get; }

    private Version(int major, int minor, int patch, string? preRelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
    }

    public static Version Create(int major, int minor, int patch, string? preRelease = null)
    {
        if (major < 0 || minor < 0 || patch < 0)
            throw new ArgumentException("Version components cannot be negative.");

        return new Version(major, minor, patch, preRelease);
    }

    public static Version Parse(string value)
    {
        if (!TryParse(value, out var version))
            throw new FormatException($"'{value}' is not a valid version string.");

        return version!;
    }

    public static bool TryParse(string? value, out Version? version)
    {
        version = null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Strip a leading 'v' or 'V' (common in GitHub tags, e.g. "v2.3.1")
        var trimmed = value.Trim();
        if (trimmed.Length > 0 && (trimmed[0] == 'v' || trimmed[0] == 'V'))
            trimmed = trimmed[1..];

        // Split off pre-release info (e.g. "2.3.1-beta.1")
        string? preRelease = null;
        var dashIndex = trimmed.IndexOf('-');
        if (dashIndex >= 0)
        {
            preRelease = trimmed[(dashIndex + 1)..];
            trimmed = trimmed[..dashIndex];
        }

        var parts = trimmed.Split('.');
        if (parts.Length < 2 || parts.Length > 3)
            return false;

        if (!int.TryParse(parts[0], out var major) || major < 0)
            return false;

        if (!int.TryParse(parts[1], out var minor) || minor < 0)
            return false;

        var patch = 0;
        if (parts.Length == 3)
        {
            if (!int.TryParse(parts[2], out patch) || patch < 0)
                return false;
        }

        version = new Version(major, minor, patch, preRelease);
        return true;
    }

    public int CompareTo(Version? other)
    {
        if (other is null) return 1;

        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;

        var patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0) return patchComparison;

        // No pre-release outranks any pre-release (1.0.0 > 1.0.0-beta)
        if (PreRelease is null && other.PreRelease is null) return 0;
        if (PreRelease is null) return 1;
        if (other.PreRelease is null) return -1;

        return string.CompareOrdinal(PreRelease, other.PreRelease);
    }

    public static bool operator >(Version a, Version b) => a.CompareTo(b) > 0;
    public static bool operator <(Version a, Version b) => a.CompareTo(b) < 0;
    public static bool operator >=(Version a, Version b) => a.CompareTo(b) >= 0;
    public static bool operator <=(Version a, Version b) => a.CompareTo(b) <= 0;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Major;
        yield return Minor;
        yield return Patch;
        yield return PreRelease;
    }

    public override string ToString() =>
        PreRelease is null
            ? $"{Major}.{Minor}.{Patch}"
            : $"{Major}.{Minor}.{Patch}-{PreRelease}";
}