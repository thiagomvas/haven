using System.Text.Json;

using Haven.Domain.ValueObjects;

namespace Haven.Infrastructure.Persistence.Configuration;

public static class QuietHoursJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize(QuietHours quietHours) =>
        JsonSerializer.Serialize(
            new StoredQuietHours(quietHours.Days.ToArray(), quietHours.Start, quietHours.End, quietHours.TimeZoneId),
            SerializerOptions);

    public static QuietHours Deserialize(string json)
    {
        var stored = JsonSerializer.Deserialize<StoredQuietHours>(json, SerializerOptions)
            ?? throw new JsonException("Quiet hours column holds no value.");

        return QuietHours.From(stored.Days, stored.Start, stored.End, stored.TimeZoneId);
    }

    private sealed record StoredQuietHours(DayOfWeek[] Days, TimeOnly Start, TimeOnly End, string TimeZoneId);
}