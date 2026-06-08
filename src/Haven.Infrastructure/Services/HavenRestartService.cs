using Haven.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Haven.Infrastructure.Services;

public class HavenRestartService(IHostApplicationLifetime lifetime) : IHavenRestartService
{
    public void Restart()
    {
        // Delay slightly so the HTTP response is flushed before the host stops.
        // Expects the process manager (Docker, systemd) to restart the process.
        _ = Task.Delay(500).ContinueWith(_ => lifetime.StopApplication());
    }
}
