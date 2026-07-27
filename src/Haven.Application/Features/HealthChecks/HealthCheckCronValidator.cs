using System.Text.RegularExpressions;

namespace Haven.Application.Features.HealthChecks;

/// <summary>
/// Lightweight structural check for 5-field cron expressions (minute hour day-of-month month day-of-week).
/// Kept dependency-free (no Cronos/Hangfire reference) since the Application layer must not depend on
/// Infrastructure-only packages; the actual scheduling engine (Hangfire/Cronos) will reject anything
/// that slips past this at job-registration time in Infrastructure.
/// </summary>
public static partial class HealthCheckCronValidator
{
    [GeneratedRegex(@"^[0-9*/,\-]+$")]
    private static partial Regex FieldPattern();

    public static bool IsValid(string? cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return true;

        var fields = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return fields.Length == 5 && fields.All(f => FieldPattern().IsMatch(f));
    }
}
