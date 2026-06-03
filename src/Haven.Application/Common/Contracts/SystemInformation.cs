namespace Haven.Application.Common.Contracts;

public class SystemInformation
{
    public string OperatingSystem { get; init; } = string.Empty;
    public string OperatingSystemVersion { get; init; } = string.Empty;
    public TimeSpan Uptime { get; init; }
    public int CpuCores { get; init; }
    public float RamMb { get; init; }
    public float StorageMb { get; init; }
    public string IpAddress { get; init; } = string.Empty;
}