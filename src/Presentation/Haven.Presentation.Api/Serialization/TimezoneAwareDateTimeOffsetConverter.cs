using System.Text.Json;
using System.Text.Json.Serialization;

using Haven.Application.Configuration;

using Microsoft.Extensions.Options;

namespace Haven.Presentation.Api.Serialization;

public sealed class TimezoneAwareDateTimeOffsetConverter(IOptionsMonitor<InstanceOptions> options)
    : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions serializerOptions)
        => DateTimeOffset.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions serializerOptions)
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

        var converted = TimeZoneInfo.ConvertTime(value, tz);
        var format = options.CurrentValue.TimeFormat == TimeFormat.Hour24 ? "yyyy-MM-dd HH:mm:ss" : "yyyy-MM-dd hh:mm:ss tt";
        writer.WriteStringValue(converted.ToString(format));
    }
}