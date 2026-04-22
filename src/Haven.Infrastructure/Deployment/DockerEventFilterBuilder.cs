namespace Haven.Infrastructure.Deployment;

public static class DockerEventFilterBuilder
{
    public static IDictionary<string, IDictionary<string, bool>> ForEvents(params string[] events)
    {
        return new Dictionary<string, IDictionary<string, bool>>
        {
            {
                "event",
                events.ToDictionary(e => e, _ => true)
            }
        };
    }

    public static IDictionary<string, IDictionary<string, bool>> ForCrashes()
    {
        return ForEvents(DockerEventTypes.CrashLikeEvents.ToArray());
    }
}