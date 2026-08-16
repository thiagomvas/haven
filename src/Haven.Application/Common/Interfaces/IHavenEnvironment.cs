namespace Haven.Application.Common.Interfaces;

/// <summary>
/// The hosting environment Haven's own API process is running under (e.g. ASPNETCORE_ENVIRONMENT),
/// as opposed to a Project's Environment aggregate.
/// </summary>
public interface IHavenEnvironment
{
    bool IsDevelopment { get; }
}
