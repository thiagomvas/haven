using Haven.Domain.Exceptions;

namespace Haven.Domain.ValueObjects;

public sealed class QuietHours : ValueObject
{
    private const int MaxChainedWindows = 8;
    private const int MaxGapMinutes = 24 * 60;

    public IReadOnlyList<DayOfWeek> Days { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public string TimeZoneId { get; }

    private TimeZoneInfo Zone { get; }

    private QuietHours(IReadOnlyList<DayOfWeek> days, TimeOnly start, TimeOnly end, string timeZoneId, TimeZoneInfo zone)
    {
        Days = days;
        Start = start;
        End = end;
        TimeZoneId = timeZoneId;
        Zone = zone;
    }

    public static QuietHours From(IEnumerable<DayOfWeek> days, TimeOnly start, TimeOnly end, string timeZoneId)
    {
        var distinctDays = days.Distinct().Order().ToList();
        if (distinctDays.Count == 0)
            throw new ValidationException("Quiet hours need at least one day of the week.");

        if (distinctDays.Any(day => !Enum.IsDefined(day)))
            throw new ValidationException("Quiet hours contain an unknown day of the week.");

        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ValidationException("Quiet hours need a time zone.");

        return new(distinctDays, start, end, timeZoneId, ResolveZone(timeZoneId));
    }

    public bool IsQuietAt(DateTime utcNow) => EndsAt(utcNow) is not null;

    public DateTime? EndsAt(DateTime utcNow)
    {
        var now = AsUtc(utcNow);
        var end = ActiveWindowEnd(now);
        if (end is null)
            return null;

        for (var chained = 0; chained < MaxChainedWindows; chained++)
        {
            var next = ActiveWindowEnd(end.Value);
            if (next is null || next.Value <= end.Value)
                break;

            end = next;
        }

        return end;
    }

    private DateTime? ActiveWindowEnd(DateTime utc)
    {
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, Zone));

        foreach (var startDate in new[] { localDate.AddDays(-1), localDate })
        {
            if (!Days.Contains(startDate.DayOfWeek))
                continue;

            var endDate = End > Start ? startDate : startDate.AddDays(1);
            var windowStart = ToUtc(startDate.ToDateTime(Start), preferEarliest: true);
            var windowEnd = ToUtc(endDate.ToDateTime(End), preferEarliest: false);

            if (windowStart <= utc && utc < windowEnd)
                return windowEnd;
        }

        return null;
    }

    private DateTime ToUtc(DateTime local, bool preferEarliest)
    {
        var shifted = local;
        for (var minutes = 0; Zone.IsInvalidTime(shifted) && minutes < MaxGapMinutes; minutes++)
            shifted = shifted.AddMinutes(1);

        if (!Zone.IsAmbiguousTime(shifted))
            return TimeZoneInfo.ConvertTimeToUtc(shifted, Zone);

        var offsets = Zone.GetAmbiguousTimeOffsets(shifted);
        var offset = preferEarliest ? offsets.Max() : offsets.Min();
        return DateTime.SpecifyKind(shifted - offset, DateTimeKind.Utc);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static TimeZoneInfo ResolveZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new ValidationException($"'{timeZoneId}' is not a known time zone.", exception);
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return string.Join(',', Days);
        yield return Start;
        yield return End;
        yield return TimeZoneId;
    }
}