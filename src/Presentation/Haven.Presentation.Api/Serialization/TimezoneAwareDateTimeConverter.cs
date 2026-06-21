using System.Text.Json;
using System.Text.Json.Serialization;

using Haven.Application.Configuration;

using Microsoft.Extensions.Options;

namespace Haven.Presentation.Api.Serialization;

public sealed class TimezoneAwareDateTimeConverter(IOptionsMonitor<InstanceOptions> options)
    : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions serializerOptions)
        => DateTime.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions serializerOptions)
    {
        var tzId = options.CurrentValue.Timezone;
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }
        catch
        {
            tz = TimeZoneInfo.Utc;
        }

        var utc = new DateTimeOffset(value, TimeSpan.Zero);
        var converted = TimeZoneInfo.ConvertTime(utc, tz);
        writer.WriteStringValue(converted.ToString("yyyy-MM-ddTHH:mm:sszzz"));
    }
}