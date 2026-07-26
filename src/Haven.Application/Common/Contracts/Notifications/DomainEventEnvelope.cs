using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Haven.Application.Common.Contracts.Notifications;

public partial class DomainEventEnvelope
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; }
    [JsonPropertyName("occuredAt")]
    public DateTime OccuredAt { get; set; }
    [JsonPropertyName("message")]
    public string Message { get; set; }
    [JsonPropertyName("data")]
    public JsonNode? Data { get; set; }

    public string ToFormattedEventName()
    {
        if (string.IsNullOrWhiteSpace(EventType))
            return string.Empty;

        var formattedName = EventType;
        if (formattedName.EndsWith("Event"))
            formattedName = formattedName.Substring(0, formattedName.Length - "Event".Length);

        formattedName = CamelCaseRegex().Replace(formattedName, " $1");

        return formattedName;
    }

    [GeneratedRegex("(\\B[A-Z])")]
    private static partial Regex CamelCaseRegex();
}