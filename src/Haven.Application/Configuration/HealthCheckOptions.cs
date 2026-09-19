namespace Haven.Application.Configuration;

public class HealthCheckOptions
{
    public const string SectionName = "HealthChecks";

    /// <summary>
    /// Image used for the short-lived probe container that runs HTTP/TCP checks from inside the target
    /// service's environment network. Must provide <c>curl</c> and <c>nc</c> (BusyBox), as the curlimages/curl image does.
    /// </summary>
    public string ProbeImage { get; set; } = "curlimages/curl:8.11.1";

    /// <summary>Number of most recent results kept per health check.</summary>
    public int ResultRetentionCount { get; set; } = 50;

    /// <summary>Delay between attempts when a check has retries configured.</summary>
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>Upper bound for a probe container's lifetime, on top of the check's own timeout.</summary>
    public int ProbeStartupGraceSeconds { get; set; } = 30;
}
