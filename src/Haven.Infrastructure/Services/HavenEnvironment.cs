using Haven.Application.Common.Interfaces;

using Microsoft.Extensions.Hosting;

namespace Haven.Infrastructure.Services;

public sealed class HavenEnvironment(IHostEnvironment hostEnvironment) : IHavenEnvironment
{
    public bool IsDevelopment => hostEnvironment.IsDevelopment();
}
