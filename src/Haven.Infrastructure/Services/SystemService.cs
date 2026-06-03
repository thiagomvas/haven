using System.Net.NetworkInformation;
using System.Net.Sockets;
using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;

namespace Haven.Infrastructure.Services;

public class SystemService : ISystemService
{
    public async Task<Result<SystemInformation>> GetSystemInformationAsync(CancellationToken ct)
    {
        var os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        var osVersion = Environment.OSVersion.VersionString;
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        var cpuCores = Environment.ProcessorCount;
        var ipAddress = GetLocalIpAddress();
        var ramMb = GetAvailableRamMb();
        var storageMb = GetAvailableStorageMb();

        var info = new SystemInformation()
        {
            OperatingSystem = os,
            OperatingSystemVersion = osVersion,
            Uptime = uptime,
            CpuCores = cpuCores,
            IpAddress = ipAddress,
            RamMb = ramMb,
            StorageMb = storageMb
        };

        return info;
    }

    private string GetLocalIpAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            var ipv4 = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork
                                      && !System.Net.IPAddress.IsLoopback(ip));

            return ipv4?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private float GetAvailableRamMb()
    {
        try
        {
            return GC.GetTotalMemory(false) / (1024f * 1024f);
        }
        catch
        {
            return 0;
        }
    }

    private float GetAvailableStorageMb()
    {
        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
            return drive?.AvailableFreeSpace / (1024f * 1024f) ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}